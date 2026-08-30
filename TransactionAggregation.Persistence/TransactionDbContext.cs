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

      protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>()
            .HasIndex(transaction => new
            {
                transaction.Source,
                transaction.ExternalTransactionId
            })
            .IsUnique()
            .HasDatabaseName("UX_Transactions_Source_ExternalTransactionId");
    }
}