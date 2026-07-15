using Bobeta.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bobeta.Persistence.Configurations;

/// <summary>EF Core mapping for the PlayerNotification entity.</summary>
public class PlayerNotificationConfiguration : IEntityTypeConfiguration<PlayerNotification>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PlayerNotification> builder)
    {
        builder.ToTable("PlayerNotifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.ActorName).HasMaxLength(100);
        builder.Property(n => n.Amount).HasPrecision(18, 2);
        builder.Property(n => n.DeepLink).HasMaxLength(200);
        builder.Property(n => n.Type).IsRequired();
        builder.HasIndex(n => new { n.PlayerId, n.CreatedAt });
        builder.HasIndex(n => new { n.PlayerId, n.IsRead });
    }
}
