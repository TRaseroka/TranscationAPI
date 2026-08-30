using AutoMapper;
using TransactionAggregation.Contracts;
using TransactionAggregation.Persistence.Repositories;
using TransactionAggregation.Application.Exceptions;
using TransactionAggregation.Application.Interfaces;
using TransactionAggregation.Domain;

namespace TransactionAggregation.Application.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _repository;
     private readonly IMapper _mapper;
    public TransactionService(ITransactionRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper=mapper;
    }

    public async Task<Transaction?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetByIdAsync(
            id,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Transaction>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetAllAsync(
            cancellationToken);
    }

    public async Task<IReadOnlyList<Transaction>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetByCustomerIdAsync(
            customerId,
            cancellationToken);
    }

    public async Task<CustomerTransactionSummary> GetCustomerSummaryAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetCustomerSummaryAsync(
            customerId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentMethodSummary>> GetPaymentMethodSummaryAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetPaymentMethodSummaryAsync(
            customerId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<TransactionDirectionSummary>> GetTransactionDirectionSummaryAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetTransactionDirectionSummaryAsync(
            customerId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Transaction>> GetByCustomerAsync(
        Guid customerId,
        DateTime? from,
        DateTime? to,
        PaymentMethod? paymentMethod,
        TransactionDirection? direction,
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetByCustomerAsync(
            customerId,
            from,
            to,
            paymentMethod,
            direction,
            cancellationToken);
    }

    public  async Task ProcessTransaction(TransactionMessage message, CancellationToken cancellationToken = default)
    {
      var transaction = _mapper.Map<Transaction>(message);
      ValidateTransaction(transaction);

      await _repository.AddAsync(transaction,cancellationToken); 
      await _repository.SaveChangesAsync(cancellationToken);

    }
    private static void ValidateTransaction(Transaction transaction)
    {
        if (transaction.Id == Guid.Empty)
            throw new TransactionValidationException(
                "Transaction ID is required.");

        if (transaction.CustomerId == Guid.Empty)
            throw new TransactionValidationException(
                "Customer ID is required.");

        if (string.IsNullOrWhiteSpace(transaction.Source))
            throw new TransactionValidationException(
                "Transaction source is required.");

        if (string.IsNullOrWhiteSpace(transaction.ExternalTransactionId))
            throw new TransactionValidationException(
                "External transaction ID is required.");

        if (transaction.TransactionDate == default)
            throw new TransactionValidationException(
                "Transaction date is required.");

        if (transaction.Amount <= 0)
           throw new TransactionValidationException(
           "Transaction amount must be greater than zero.");

        if (string.IsNullOrWhiteSpace(transaction.Currency))
            throw new TransactionValidationException(
                "Currency is required.");

        if (!Enum.IsDefined(transaction.PaymentMethod))
            throw new TransactionValidationException(
                "Invalid payment method.");

        if (!Enum.IsDefined(transaction.Direction))
            throw new TransactionValidationException(
                "Invalid transaction direction.");
    }

  
}