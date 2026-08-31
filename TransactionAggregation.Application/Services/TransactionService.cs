using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TransactionAggregation.Contracts;
using TransactionAggregation.Persistence.Repositories;
using TransactionAggregation.Application.Exceptions;
using TransactionAggregation.Application.Interfaces;
using TransactionAggregation.Domain;
using TransactionAggregation.Contracts.Transactions;

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

   public async Task<TransactionResponseDto?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken = default)
{
    var transaction = await _repository.GetByIdAsync(
        id,
        cancellationToken);

    return transaction is null
        ? null
        : _mapper.Map<TransactionResponseDto>(transaction);
}

public async Task<IReadOnlyList<TransactionResponseDto>> GetAllAsync(
    CancellationToken cancellationToken = default)
{
    var transactions = await _repository.GetAllAsync(
        cancellationToken);

    return _mapper.Map<IReadOnlyList<TransactionResponseDto>>(
        transactions);
}

    public async Task<IReadOnlyList<TransactionResponseDto>> GetByCustomerIdAsync(
    Guid customerId,
    CancellationToken cancellationToken = default)
{
    var transactions = await _repository.GetByCustomerIdAsync(
        customerId,
        cancellationToken);

    return _mapper.Map<IReadOnlyList<TransactionResponseDto>>(
        transactions);
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

  public async Task<IReadOnlyList<TransactionResponseDto>> GetByCustomerAsync(
    Guid customerId,
    DateTime? from,
    DateTime? to,
    PaymentMethod? paymentMethod,
    TransactionDirection? direction,
    CancellationToken cancellationToken = default)
{
        if (from.HasValue && to.HasValue && from > to)
    {
        throw new TransactionValidationException(
            "'from' date cannot be later than 'to' date.");
    }
    var transactions = await _repository.GetByCustomerAsync(
        customerId,
        from,
        to,
        paymentMethod,
        direction,
        cancellationToken);

    return _mapper.Map<IReadOnlyList<TransactionResponseDto>>(
        transactions);
}

    public  async Task ProcessTransaction(TransactionMessage message, CancellationToken cancellationToken = default)
    {
      var transaction = _mapper.Map<Transaction>(message);

      ValidateTransaction(transaction);
        try{
      await _repository.AddAsync(transaction,cancellationToken); 
      await _repository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        when (exception.InnerException is PostgresException postgresException &&
              postgresException.ConstraintName == "PK_Transactions")
      {
        throw new TransactionDuplicateException(
            TransactionDuplicateType.TransactionId,
            $"Transaction with ID '{transaction.Id}' already exists.",
            exception);
      }
    catch (DbUpdateException exception)
        when (exception.InnerException is PostgresException postgresException &&
              postgresException.ConstraintName ==
                  "UX_Transactions_Source_ExternalTransactionId")
    {
        throw new TransactionDuplicateException(
            TransactionDuplicateType.SourceAndExternalTransactionId,
            $"Transaction with source '{transaction.Source}' " +
            $"and external transaction ID '{transaction.ExternalTransactionId}' already exists.",
            exception);
    }

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