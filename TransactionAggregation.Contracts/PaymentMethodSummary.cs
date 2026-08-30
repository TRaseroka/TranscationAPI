using System.Text.Json.Serialization;
using TransactionAggregation.Domain;
namespace TransactionAggregation.Contracts;

public class PaymentMethodSummary
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PaymentMethod PaymentMethod { get; set; }

    public int TransactionCount { get; set; }

    public decimal TotalAmount { get; set; }
}