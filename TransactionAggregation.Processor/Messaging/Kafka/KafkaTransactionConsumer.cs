using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using TransactionAggregation.Application.Exceptions;
using TransactionAggregation.Contracts;
using TransactionAggregation.Processor.Messaging;
using TransactionAggregation.Application.Serialization;

namespace TransactionAggregation.Processor.Kafka;

public class KafkaTransactionConsumer : BackgroundService
{
    private readonly KafkaOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KafkaTransactionConsumer> _logger;
    private readonly TransactionXmlDeserializer _xmlDeserializer;

    public KafkaTransactionConsumer(
        IOptions<KafkaOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<KafkaTransactionConsumer> logger,
        TransactionXmlDeserializer xmlDeserializer)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _xmlDeserializer = xmlDeserializer;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers
        };

        using var consumer =
            new ConsumerBuilder<Ignore, string>(consumerConfig)
                .Build();

        using var producer =
            new ProducerBuilder<Null, string>(producerConfig)
                .Build();

        consumer.Subscribe(_options.Topic);

        _logger.LogInformation(
            "Kafka consumer listening on topic {Topic}",
            _options.Topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<Ignore, string>? result = null;

                try
                {
                    result = consumer.Consume(stoppingToken);

                    if (result?.Message?.Value is null)
                        continue;

                    _logger.LogInformation(
                        "Received Kafka transaction message: {Message}",
                        result.Message.Value);

                 var transactionMessage =
                    _xmlDeserializer.Deserialize(
                     result.Message.Value);


                    try
                    {
                        await ProcessWithRetryAsync(
                            transactionMessage,
                            stoppingToken);
                    }
                    catch (TransactionDuplicateException ex)
                    {
                        _logger.LogWarning(
                            "Duplicate Kafka transaction detected. " +
                            "Type: {DuplicateType}. " +
                            "Message will be committed and skipped.",
                            ex.DuplicateType);

                        consumer.Commit(result);
                        continue;
                    }
                    catch (TransactionValidationException ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Invalid Kafka transaction XML. " +
                            "Message will be committed and skipped.");

                        consumer.Commit(result);
                        continue;
                    }

                    consumer.Commit(result);

                    _logger.LogInformation(
                        "Kafka transaction {TransactionId} processed successfully.",
                        transactionMessage.TransactionId);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(
                        ex,
                        "Kafka consume error.");
                }
        catch (TransactionXmlDeserializationException ex)
      {
             _logger.LogError(
             ex,
              "Invalid XML received from Kafka. Message will be committed and skipped.");
    if (result != null)
    {
        await PublishInvalidXmlToDeadLetterTopicAsync(
            result.Message.Value,
            ex,
            stoppingToken);

        consumer.Commit(result);
      }
      
      }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Unexpected Kafka processing error.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(
                "Kafka consumer cancellation requested.");
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task ProcessWithRetryAsync(
        TransactionMessage transactionMessage,
        CancellationToken cancellationToken)
    {
        var totalAttempts = _options.MaxRetries + 1;

        for (var attempt = 1; attempt <= totalAttempts; attempt++)
        {
            try
            {
               
                using var scope =
                    _scopeFactory.CreateScope();

                var handler =
                    scope.ServiceProvider
                        .GetRequiredService<ITransactionMessageHandler>();

                await handler.HandleAsync(
                    transactionMessage,
                    cancellationToken);

                return;
            }
            catch (TransactionDuplicateException)
            {
                throw;
            }
            catch (TransactionValidationException)
            {
                throw;
            }
            catch (Exception ex) when (attempt < totalAttempts)
            {
                var delaySeconds =
                    _options.RetryDelaySeconds *
                    (int)Math.Pow(2, attempt - 1);

                _logger.LogWarning(
                    ex,
                    "Kafka transaction {TransactionId} failed on " +
                    "attempt {Attempt}/{TotalAttempts}. " +
                    "Retrying in {DelaySeconds} seconds.",
                    transactionMessage.TransactionId,
                    attempt,
                    totalAttempts,
                    delaySeconds);

                await Task.Delay(
                    TimeSpan.FromSeconds(delaySeconds),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Kafka transaction {TransactionId} failed after " +
                    "{TotalAttempts} attempts. Sending message to DLQ.",
                    transactionMessage.TransactionId,
                    totalAttempts);

                await PublishToDeadLetterTopicAsync(
                    transactionMessage,
                    ex,
                    cancellationToken);

                return;
            }
        }
    }

    private async Task PublishToDeadLetterTopicAsync(
        TransactionMessage transactionMessage,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var dlqMessage = new
        {
            Transaction = transactionMessage,
            Error = exception.Message,
            ExceptionType = exception.GetType().Name,
            FailedAtUtc = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(dlqMessage);

        var message = new Message<Null, string>
        {
            Value = json
        };

        await PublishAsync(
            message,
            cancellationToken);
    }
private async Task PublishInvalidXmlToDeadLetterTopicAsync(
    string rawXml,
    Exception exception,
    CancellationToken cancellationToken)
{
    var dlqMessage = new
    {
        RawMessage = rawXml,
        Error = exception.Message,
        ExceptionType = exception.GetType().Name,
        FailedAtUtc = DateTime.UtcNow
    };

    var json = JsonSerializer.Serialize(dlqMessage);

    var message = new Message<Null, string>
    {
        Value = json
    };

    await PublishAsync(
        message,
        cancellationToken);
}
    private async Task PublishAsync(
        Message<Null, string> message,
        CancellationToken cancellationToken)
    {
        using var producer =
            new ProducerBuilder<Null, string>(
                new ProducerConfig
                {
                    BootstrapServers = _options.BootstrapServers
                })
            .Build();

        var deliveryResult =
            await producer.ProduceAsync(
                _options.DeadLetterTopic,
                message,
                cancellationToken);

        _logger.LogWarning(
            "Kafka message published to DLQ topic {Topic}, " +
            "partition {Partition}, offset {Offset}.",
            _options.DeadLetterTopic,
            deliveryResult.Partition,
            deliveryResult.Offset);
    }
}