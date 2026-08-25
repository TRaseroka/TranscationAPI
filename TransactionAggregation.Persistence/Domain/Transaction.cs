using TransactionAggregation.Contracts;
namespace TransactionAggregation.Persistence.Domain;

public class Transaction
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public string Source { get; set; } = string.Empty;

    public string ExternalTransactionId { get; set; } = string.Empty;

    public DateTime TransactionDate { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public PaymentMethod PaymentMethod { get; set; }

    public TransactionDirection Direction { get; set; }
}
