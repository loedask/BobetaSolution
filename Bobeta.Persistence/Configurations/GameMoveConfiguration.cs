using Bobeta.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bobeta.Persistence.Configurations;

/// <summary>EF Core mapping for the GameMove entity.</summary>
public class GameMoveConfiguration : IEntityTypeConfiguration<GameMove>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<GameMove> builder)
    {
        builder.ToTable("GameMoves");
        builder.HasKey(m => m.Id);
        // Domino/Kopo notations exceed the original Makopa "Suit_Rank" budget (20).
        builder.Property(m => m.CardSuitRank).HasMaxLength(64);
        builder.HasOne(m => m.GameSession).WithMany(s => s.GameMoves).HasForeignKey(m => m.GameSessionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(m => m.Player).WithMany().HasForeignKey(m => m.PlayerId).OnDelete(DeleteBehavior.Restrict);
    }
}
