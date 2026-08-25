using Microsoft.EntityFrameworkCore;
using TransactionAggregation.Contracts;
using TransactionAggregation.Persistence;
using TransactionAggregation.Persistence.Domain;

namespace TransactionAggregation.Persistence.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly TransactionDbContext _dbContext;

    public TransactionRepository(TransactionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Transaction?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Transactions
            .FirstOrDefaultAsync(
                transaction => transaction.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Transaction>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Transactions
            .AsNoTracking()
            .OrderByDescending(transaction => transaction.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Transactions.AddAsync(
            transaction,
            cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

     public async Task<IReadOnlyList<Transaction>> GetByCustomerIdAsync(
    Guid customerId,
    CancellationToken cancellationToken = default)
{
    return await _dbContext.Transactions
        .AsNoTracking()
        .Where(transaction => transaction.CustomerId == customerId)
        .OrderByDescending(transaction => transaction.TransactionDate)
        .ToListAsync(cancellationToken);
}

public async Task<CustomerTransactionSummary> GetCustomerSummaryAsync(
    Guid customerId,
    CancellationToken cancellationToken = default)
{
    var transactions = await _dbContext.Transactions
        .AsNoTracking()
        .Where(transaction => transaction.CustomerId == customerId)
        .ToListAsync(cancellationToken);

    var totalCredits = transactions
        .Where(transaction => transaction.Direction == TransactionDirection.Credit)
        .Sum(transaction => transaction.Amount);

    var totalDebits = transactions
        .Where(transaction => transaction.Direction == TransactionDirection.Debit)
        .Sum(transaction => transaction.Amount);

    return new CustomerTransactionSummary
    {
        CustomerId = customerId,
        TransactionCount = transactions.Count,
        TotalCredits = totalCredits,
        TotalDebits = totalDebits,
        NetAmount = totalCredits - totalDebits
    };
}

public async Task<IReadOnlyList<PaymentMethodSummary>> GetPaymentMethodSummaryAsync(
    Guid customerId,
    CancellationToken cancellationToken = default)
{
    return await _dbContext.Transactions
        .AsNoTracking()
        .Where(transaction => transaction.CustomerId == customerId)
        .GroupBy(transaction => transaction.PaymentMethod)
        .Select(group => new PaymentMethodSummary
        {
            PaymentMethod = group.Key,
            TransactionCount = group.Count(),
            TotalAmount = group.Sum(transaction => transaction.Amount)
        })
        .OrderByDescending(summary => summary.TotalAmount)
        .ToListAsync(cancellationToken);
}

public async Task<IReadOnlyList<TransactionDirectionSummary>> GetTransactionDirectionSummaryAsync(
    Guid customerId,
    CancellationToken cancellationToken = default)
{
    return await _dbContext.Transactions
        .AsNoTracking()
        .Where(transaction => transaction.CustomerId == customerId)
        .GroupBy(transaction => transaction.Direction)
        .Select(group => new TransactionDirectionSummary
        {
            Direction = group.Key,
            TransactionCount = group.Count(),
            TotalAmount = group.Sum(transaction => transaction.Amount)
        })
        .OrderByDescending(summary => summary.TotalAmount)
        .ToListAsync(cancellationToken);
}

public async Task<IReadOnlyList<Transaction>> GetByCustomerAsync(
    Guid customerId,
    DateTime? from,
    DateTime? to,
    PaymentMethod? paymentMethod,
    TransactionDirection? direction,
    CancellationToken cancellationToken = default)
{
    var query = _dbContext.Transactions
        .AsNoTracking()
        .Where(transaction => transaction.CustomerId == customerId);

    if (from.HasValue)
    {
        query = query.Where(
            transaction => transaction.TransactionDate >= from.Value);
    }

    if (to.HasValue)
    {
        query = query.Where(
            transaction => transaction.TransactionDate <= to.Value);
    }

    if (paymentMethod.HasValue)
    {
        query = query.Where(
            transaction => transaction.PaymentMethod == paymentMethod.Value);
    }

    if (direction.HasValue)
    {
        query = query.Where(
            transaction => transaction.Direction == direction.Value);
    }

    return await query
        .OrderByDescending(transaction => transaction.TransactionDate)
        .ToListAsync(cancellationToken);
}
}