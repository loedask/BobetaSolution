using Bobeta.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bobeta.Persistence.Configurations;

/// <summary>EF Core mapping for the Wallet entity (precision for decimals, unique player index, cascade).</summary>
public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("Wallets");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Balance).HasPrecision(18, 2);
        builder.Property(w => w.LockedBalance).HasPrecision(18, 2);
        builder.HasOne(w => w.Player).WithMany().HasForeignKey(w => w.PlayerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(w => w.PlayerId).IsUnique();
    }
}
