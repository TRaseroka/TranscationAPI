using TransactionAggregation.Contracts;
using TransactionAggregation.Domain;

namespace TransactionAggregation.Application.Interfaces;


public interface ITransactionService
{   
    Task<Transaction?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Transaction>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Transaction>> GetByCustomerIdAsync(
    Guid customerId,
    CancellationToken cancellationToken = default);

    Task<CustomerTransactionSummary> GetCustomerSummaryAsync(
    Guid customerId,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<PaymentMethodSummary>> GetPaymentMethodSummaryAsync(
    Guid customerId,
    CancellationToken cancellationToken = default);
Task<IReadOnlyList<TransactionDirectionSummary>> GetTransactionDirectionSummaryAsync(
    Guid customerId,
    CancellationToken cancellationToken = default);
   
   Task<IReadOnlyList<Transaction>> GetByCustomerAsync(
    Guid customerId,
    DateTime? from,
    DateTime? to,
    PaymentMethod? paymentMethod,
    TransactionDirection? direction,
    CancellationToken cancellationToken = default);
    Task ProcessTransaction(
        TransactionMessage message,
        CancellationToken cancellationToken = default);
}
