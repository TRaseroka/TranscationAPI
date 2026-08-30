using Microsoft.EntityFrameworkCore;
using TransactionAggregation.Domain;

namespace TransactionAggregation.Persistence;

public class TransactionDbContext : DbContext
{
    public TransactionDbContext(
        DbContextOptions<TransactionDbContext> options)
        : base(options)
    {
    }

    public DbSet<Transaction> Transactions => Set<Transaction>();
}