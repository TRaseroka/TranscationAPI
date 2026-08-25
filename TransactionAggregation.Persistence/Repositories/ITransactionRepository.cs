using TransactionAggregation.Persistence.Domain;
using TransactionAggregation.Contracts;


namespace TransactionAggregation.Persistence.Repositories;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Transaction>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
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
}