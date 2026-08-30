namespace TransactionAggregation.Processor.Messaging.RabbitMQ;

public class RabbitMqOptions
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string QueueName { get; set; } = "transactions";

    public string RetryQueueName { get; set; } = "transactions.retry";

    public string DeadLetterQueueName { get; set; } = "transactions.dlq";

    public int RetryDelayMilliseconds { get; set; } = 5000;

    public int MaxRetries { get; set; } = 3;
}