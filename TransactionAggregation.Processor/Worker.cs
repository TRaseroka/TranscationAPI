using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TransactionAggregation.Application.Exceptions;
using TransactionAggregation.Application.Interfaces;
using TransactionAggregation.Contracts;
using TransactionAggregation.Processor.Messaging.RabbitMQ;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace TransactionAggregation.Processor;

public class Worker : BackgroundService
{
private readonly ILogger<Worker> _logger;
private readonly RabbitMqOptions _options;
private readonly IServiceScopeFactory _scopeFactory;


private IConnection? _connection;
private IChannel? _channel;

public Worker(
    ILogger<Worker> logger,
    RabbitMqOptions options,
    IServiceScopeFactory scopeFactory)
{
    _logger = logger;
    _options = options;
    _scopeFactory = scopeFactory;
}

protected override async Task ExecuteAsync(
    CancellationToken stoppingToken)
{
    var factory = new ConnectionFactory
    {
        HostName = _options.Host,
        Port = _options.Port,
        UserName = _options.Username,
        Password = _options.Password
    };

    _connection = await factory.CreateConnectionAsync(
        stoppingToken);

    _channel = await _connection.CreateChannelAsync(
        cancellationToken: stoppingToken);

    await _channel.QueueDeclareAsync(
        queue: _options.QueueName,
        durable: true,
        exclusive: false,
        autoDelete: false,
        cancellationToken: stoppingToken);

    await _channel.BasicQosAsync(
        prefetchSize: 0,
        prefetchCount: 1,
        global: false,
        cancellationToken: stoppingToken);

    _logger.LogInformation(
        "Connected to RabbitMQ at {Host}:{Port}",
        _options.Host,
        _options.Port);

    _logger.LogInformation(
        "Listening on queue: {QueueName}",
        _options.QueueName);

    var consumer = new AsyncEventingBasicConsumer(_channel);

    consumer.ReceivedAsync += async (_, eventArgs) =>
    {
        try
        {
            var body = eventArgs.Body.ToArray();

            var json = Encoding.UTF8.GetString(body);

            _logger.LogInformation(
                "Received transaction message: {Message}",
                json);

            var message =
                JsonSerializer.Deserialize<TransactionMessage>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters =
                        {
                            new JsonStringEnumConverter()
                        }
                    });

            if (message is null)
            {
                throw new InvalidOperationException(
                    "Transaction message could not be deserialized.");
            }

            using var scope =
                _scopeFactory.CreateScope();

            var service =
                scope.ServiceProvider
                    .GetRequiredService<ITransactionService>();

            await service.ProcessTransaction(
                message,
                stoppingToken);

            await _channel.BasicAckAsync(
                eventArgs.DeliveryTag,
                multiple: false,
                cancellationToken: stoppingToken);

            _logger.LogInformation(
                "Processed transaction {TransactionId} successfully.",
                message.TransactionId);
        }
        catch (TransactionValidationException exception)
        {
            _logger.LogError(
                exception,
                "Invalid transaction message. Message will not be requeued.");

            await _channel.BasicNackAsync(
                eventArgs.DeliveryTag,
                multiple: false,
                requeue: false,
                cancellationToken: stoppingToken);
        }
        catch (DbUpdateException exception)
          when (exception.InnerException is PostgresException postgresException &&
          postgresException.SqlState == "23505")
       {
        _logger.LogWarning(
        exception,
        "Duplicate transaction detected. Message has already been processed.");

        await _channel.BasicAckAsync(
        eventArgs.DeliveryTag,
        multiple: false,
        cancellationToken: stoppingToken);
        }

        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Error processing RabbitMQ transaction message. Message will be requeued.");

            await _channel.BasicNackAsync(
                eventArgs.DeliveryTag,
                multiple: false,
                requeue: true,
                cancellationToken: stoppingToken);
        }
    };

    await _channel.BasicConsumeAsync(
        queue: _options.QueueName,
        autoAck: false,
        consumer: consumer,
        cancellationToken: stoppingToken);

    try
    {
        await Task.Delay(
            Timeout.Infinite,
            stoppingToken);
    }
    catch (OperationCanceledException)
    {
        // Normal shutdown.
    }
}

public override async Task StopAsync(
    CancellationToken cancellationToken)
{
    if (_channel is not null)
    {
        await _channel.CloseAsync(cancellationToken);
    }

    if (_connection is not null)
    {
        await _connection.CloseAsync(cancellationToken);
    }

    await base.StopAsync(cancellationToken);
}

}
