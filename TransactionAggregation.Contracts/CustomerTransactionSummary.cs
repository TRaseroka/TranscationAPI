namespace TransactionAggregation.Contracts;

public class CustomerTransactionSummary
{
    public Guid CustomerId { get; set; }

    public int TransactionCount { get; set; }

    public decimal TotalCredits { get; set; }

    public decimal TotalDebits { get; set; }

    public decimal NetAmount { get; set; }
}