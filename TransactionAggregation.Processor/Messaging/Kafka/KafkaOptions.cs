namespace TransactionAggregation.Processor.Kafka;

public class KafkaOptions
{
    public string BootstrapServers { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;

    public string DeadLetterTopic { get; set; } = "transactions.dlq";

    public int MaxRetries { get; set; } = 3;

    public int RetryDelaySeconds { get; set; } = 2;
}