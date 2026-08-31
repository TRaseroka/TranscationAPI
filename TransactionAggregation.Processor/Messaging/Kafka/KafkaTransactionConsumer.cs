using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using TransactionAggregation.Application.Exceptions;
using TransactionAggregation.Contracts;
using TransactionAggregation.Processor.Messaging;

namespace TransactionAggregation.Processor.Kafka;

public class KafkaTransactionConsumer : BackgroundService
{
    private readonly KafkaOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KafkaTransactionConsumer> _logger;

    public KafkaTransactionConsumer(
        IOptions<KafkaOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<KafkaTransactionConsumer> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
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

        using var consumer =
            new ConsumerBuilder<Ignore, string>(consumerConfig)
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
                        JsonSerializer.Deserialize<TransactionMessage>(
                            result.Message.Value,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true,
                                Converters =
                                {
                                    new JsonStringEnumConverter()
                                }
                            });

                    if (transactionMessage is null)
                    {
                        _logger.LogWarning(
                            "Kafka message could not be deserialized.");

                        consumer.Commit(result);
                        continue;
                    }

                    using var scope =
                        _scopeFactory.CreateScope();

                    var handler =
                        scope.ServiceProvider
                            .GetRequiredService<ITransactionMessageHandler>();

                    await handler.HandleAsync(
                        transactionMessage,
                        stoppingToken);

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
                catch (JsonException ex)
                {
                    _logger.LogError(
                        ex,
                        "Invalid JSON received from Kafka.");

                    if (result != null)
                        consumer.Commit(result);
                }
                catch (TransactionDuplicateException ex)
                {
                    _logger.LogWarning(
                        "Duplicate Kafka transaction detected. " +
                        "Type: {DuplicateType}. " +
                        "Message will be committed and skipped.",
                        ex.DuplicateType);

                    if (result != null)
                        consumer.Commit(result);
                }
                catch (TransactionValidationException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Invalid Kafka transaction. " +
                        "Message will be committed and skipped.");

                    if (result != null)
                        consumer.Commit(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error processing Kafka transaction.");
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
}