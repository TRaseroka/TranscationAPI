using TransactionAggregation.Contracts.Transactions;
using TransactionAggregation.Domain;
using TransactionAggregation.Contracts;

namespace TransactionAggregation.Application.Interfaces;

public interface ITransactionService
{
    Task<TransactionResponseDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransactionResponseDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransactionResponseDto>> GetByCustomerIdAsync(
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

    Task<IReadOnlyList<TransactionResponseDto>> GetByCustomerAsync(
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