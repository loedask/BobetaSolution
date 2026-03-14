using Bobeta.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bobeta.Persistence.Configurations;

/// <summary>EF Core mapping for the WalletTransaction entity.</summary>
public class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.ToTable("WalletTransactions");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Amount).HasPrecision(18, 2);
        builder.Property(t => t.Reference).HasMaxLength(100);
        builder.HasOne(t => t.Player).WithMany().HasForeignKey(t => t.PlayerId).OnDelete(DeleteBehavior.Cascade);
    }
}
