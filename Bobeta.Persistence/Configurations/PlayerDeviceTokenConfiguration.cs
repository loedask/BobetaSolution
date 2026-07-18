using Bobeta.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bobeta.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="PlayerDeviceToken"/>.</summary>
public class PlayerDeviceTokenConfiguration : IEntityTypeConfiguration<PlayerDeviceToken>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PlayerDeviceToken> builder)
    {
        builder.ToTable("PlayerDeviceTokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Token).HasMaxLength(512).IsRequired();
        builder.Property(t => t.Platform).IsRequired();
        builder.HasIndex(t => t.Token).IsUnique();
        builder.HasIndex(t => new { t.PlayerId, t.UpdatedAt });
    }
}
