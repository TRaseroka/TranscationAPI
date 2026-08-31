using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TransactionAggregation.Application.Exceptions;
using TransactionAggregation.Application.Interfaces;
using TransactionAggregation.Contracts;
using TransactionAggregation.Processor.Messaging.RabbitMQ;
using TransactionAggregation.Processor.Messaging;


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

     var retryQueueName = _options.RetryQueueName;
     var deadLetterQueueName = _options.DeadLetterQueueName;
     var mainQueueName = _options.QueueName;

    await _channel.QueueDeclareAsync(
        queue: mainQueueName,
        durable: true,
        exclusive: false,
        autoDelete: false,
        cancellationToken: stoppingToken);
    await _channel.QueueDeclareAsync(
        queue: deadLetterQueueName,
        durable: true,
        exclusive: false,
        autoDelete: false,
        cancellationToken: stoppingToken);

    var retryArguments = new Dictionary<string, object?>
    {
    ["x-message-ttl"] = _options.RetryDelayMilliseconds,
    ["x-dead-letter-exchange"] = string.Empty,
    ["x-dead-letter-routing-key"] = _options.QueueName
};

    await _channel.QueueDeclareAsync(
    queue: retryQueueName,
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: retryArguments,
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

            var handler =
                scope.ServiceProvider
                    .GetRequiredService<ITransactionMessageHandler>();

            await handler.HandleAsync(
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

    await _channel!.BasicNackAsync(
        eventArgs.DeliveryTag,
        multiple: false,
        requeue: false,
        cancellationToken: stoppingToken);
     }
    catch (TransactionDuplicateException exception)
    {
    _logger.LogWarning(
        exception,
        "Duplicate transaction detected. " +
        "Duplicate type: {DuplicateType}. " +
        "Message has already been processed.",
        exception.DuplicateType);

    await _channel!.BasicAckAsync(
        eventArgs.DeliveryTag,
        multiple: false,
        cancellationToken: stoppingToken);
    }
    catch (JsonException exception)
   {
    _logger.LogError(
        exception,
        "Invalid transaction JSON. Message will not be requeued.");

    await _channel!.BasicNackAsync(
        eventArgs.DeliveryTag,
        multiple: false,
        requeue: false,
        cancellationToken: stoppingToken);
   }
   catch (Exception exception)
    {
    _logger.LogError(
        exception,
        "Error processing RabbitMQ transaction message. " +
        "Message will be retried.");

    await RetryMessageAsync(
        eventArgs,
        stoppingToken);
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
private int GetRetryCount(BasicDeliverEventArgs eventArgs)
{
    if (eventArgs.BasicProperties?.Headers is null)
        return 0;

    if (!eventArgs.BasicProperties.Headers.TryGetValue(
            "x-retry-count",
            out var value))
        return 0;

    if (value is byte[] bytes &&
        int.TryParse(
            Encoding.UTF8.GetString(bytes),
            out var retryCount))
    {
        return retryCount;
    }

    return 0;
}

private async Task RetryMessageAsync(
    BasicDeliverEventArgs eventArgs,
    CancellationToken cancellationToken)
{
    var retryCount = GetRetryCount(eventArgs);

    if (retryCount >= _options.MaxRetries)
    {
        await _channel!.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: _options.DeadLetterQueueName,
            mandatory: false,
            basicProperties: new BasicProperties
            {
                Persistent = true
            },
            body: eventArgs.Body,
            cancellationToken: cancellationToken);

        await _channel.BasicAckAsync(
            eventArgs.DeliveryTag,
            multiple: false,
            cancellationToken: cancellationToken);

        _logger.LogError(
            "Maximum retry count reached. " +
            "Transaction message moved to dead-letter queue {QueueName}.",
            _options.DeadLetterQueueName);

        return;
    }

    var nextRetryCount = retryCount + 1;

    var properties = new BasicProperties
    {
        Persistent = true,
        Headers = new Dictionary<string, object?>
        {
            ["x-retry-count"] = nextRetryCount.ToString()
        }
    };

    await _channel!.BasicPublishAsync(
        exchange: string.Empty,
        routingKey: _options.RetryQueueName,
        mandatory: false,
        basicProperties: properties,
        body: eventArgs.Body,
        cancellationToken: cancellationToken);

    await _channel.BasicAckAsync(
        eventArgs.DeliveryTag,
        multiple: false,
        cancellationToken: cancellationToken);

    _logger.LogWarning(
        "Transaction message scheduled for retry. " +
        "Retry attempt {RetryCount} of {MaxRetries}.",
        nextRetryCount,
        _options.MaxRetries);
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
