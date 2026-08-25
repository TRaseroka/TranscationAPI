using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TransactionAggregation.Processor.Messaging.RabbitMQ;

namespace TransactionAggregation.Processor;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly RabbitMqOptions _options;

    private IConnection? _connection;
    private IChannel? _channel;

    public Worker(
        ILogger<Worker> logger,
        RabbitMqOptions options)
    {
        _logger = logger;
        _options = options;
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
            var body = eventArgs.Body.ToArray();

            var message = System.Text.Encoding.UTF8.GetString(body);

            _logger.LogInformation(
                "Received transaction message: {Message}",
                message);

            await _channel.BasicAckAsync(
                eventArgs.DeliveryTag,
                multiple: false,
                cancellationToken: stoppingToken);
        };

        await _channel.BasicConsumeAsync(
            queue: _options.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        await Task.Delay(
            Timeout.Infinite,
            stoppingToken);
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