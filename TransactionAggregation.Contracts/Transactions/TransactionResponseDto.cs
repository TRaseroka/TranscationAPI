namespace TransactionAggregation.Contracts.Transactions;

public class TransactionResponseDto
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public string Source { get; set; } = string.Empty;

    public string ExternalTransactionId { get; set; } = string.Empty;

    public DateTime TransactionDate { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string PaymentMethod { get; set; }= string.Empty;

    public string Direction { get; set; }= string.Empty;
}