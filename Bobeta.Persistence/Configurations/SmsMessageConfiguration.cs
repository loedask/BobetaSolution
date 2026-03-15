using Bobeta.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bobeta.Persistence.Configurations;

/// <summary>EF Core mapping for the SmsMessage entity.</summary>
public class SmsMessageConfiguration : IEntityTypeConfiguration<SmsMessage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SmsMessage> builder)
    {
        builder.ToTable("SmsMessages");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.PhoneNumber).HasMaxLength(20).IsRequired();
        builder.Property(s => s.Message).HasMaxLength(1600).IsRequired();
        builder.Property(s => s.ProviderMessageId).HasMaxLength(100);
        builder.Property(s => s.Status);
        builder.HasIndex(s => s.ProviderMessageId).HasFilter("\"ProviderMessageId\" IS NOT NULL");
    }
}
