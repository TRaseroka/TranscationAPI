using Microsoft.EntityFrameworkCore;
using TransactionAggregation.Persistence.Domain;

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