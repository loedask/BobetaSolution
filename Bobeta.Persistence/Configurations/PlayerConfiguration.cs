using Bobeta.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bobeta.Persistence.Configurations;

/// <summary>EF Core mapping for the Player entity (table name, key, indexes, column lengths).</summary>
public class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable("Players");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.PhoneNumber).HasMaxLength(20).IsRequired();
        builder.Property(p => p.PlayerName).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Language).HasMaxLength(10);
        builder.HasIndex(p => p.PhoneNumber).IsUnique();
    }
}
