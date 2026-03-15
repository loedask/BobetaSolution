using Bobeta.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bobeta.Persistence.Configurations;

/// <summary>EF Core mapping for PaymentTransaction (MoMo payments).</summary>
public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("PaymentTransactions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.Currency).HasMaxLength(3);
        builder.Property(p => p.ExternalReference).HasMaxLength(64);
        builder.Property(p => p.MoMoTransactionId).HasMaxLength(128);
        builder.HasOne(p => p.Player).WithMany().HasForeignKey(p => p.PlayerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => p.ExternalReference);
        builder.HasIndex(p => p.MoMoTransactionId).HasFilter("\"MoMoTransactionId\" IS NOT NULL");
    }
}
