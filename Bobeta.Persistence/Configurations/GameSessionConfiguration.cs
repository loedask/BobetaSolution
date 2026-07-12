using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bobeta.Persistence.Configurations;

/// <summary>EF Core mapping for the GameSession entity (jsonb for game state, FKs).</summary>
public class GameSessionConfiguration : IEntityTypeConfiguration<GameSession>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<GameSession> builder)
    {
        builder.ToTable("GameSessions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.BetAmount).HasPrecision(18, 2);
        builder.Property(s => s.Variant).HasConversion<int>().HasDefaultValue(GameVariant.Makopa);
        builder.Property(s => s.GameStateJson).HasColumnType("jsonb");
        builder.HasOne(s => s.CreatorPlayer).WithMany().HasForeignKey(s => s.CreatorPlayerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.OpponentPlayer).WithMany().HasForeignKey(s => s.OpponentPlayerId).OnDelete(DeleteBehavior.Restrict);
    }
}
