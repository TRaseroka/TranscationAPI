using TransactionAggregation.Domain;
namespace TransactionAggregation.Contracts;

public class TransactionDirectionSummary
{
    public TransactionDirection Direction { get; set; }

    public int TransactionCount { get; set; }

    public decimal TotalAmount { get; set; }
}