using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TransactionAggregation.Persistence.Domain;
using TransactionAggregation.Persistence.Repositories;
using TransactionAggregation.Processor.Contracts;
using TransactionAggregation.Processor.Messaging.RabbitMQ;
using System.Text.Json.Serialization;

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

                var transaction = new Transaction
                {
                    Id = message.TransactionId,
                    CustomerId = message.CustomerId,
                    Source = message.Source,
                    ExternalTransactionId =
                        message.ExternalTransactionId,
                    TransactionDate = message.TransactionDate,
                    Amount = message.Amount,
                    Currency = message.Currency,
                    Description = message.Description,
                    PaymentMethod = message.PaymentMethod,
                    Direction = message.Direction
                };

                using var scope = _scopeFactory.CreateScope();

                var repository =
                    scope.ServiceProvider
                        .GetRequiredService<ITransactionRepository>();

                await repository.AddAsync(
                    transaction,
                    stoppingToken);

                await repository.SaveChangesAsync(
                    stoppingToken);

                await _channel.BasicAckAsync(
                    eventArgs.DeliveryTag,
                    multiple: false,
                    cancellationToken: stoppingToken);

                _logger.LogInformation(
                    "Processed transaction {TransactionId} successfully.",
                    transaction.Id);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Error processing RabbitMQ transaction message.");

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