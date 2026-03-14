using Bobeta.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bobeta.Persistence.Configurations;

public class GameMoveConfiguration : IEntityTypeConfiguration<GameMove>
{
    public void Configure(EntityTypeBuilder<GameMove> builder)
    {
        builder.ToTable("GameMoves");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.CardSuitRank).HasMaxLength(20);
        builder.HasOne(m => m.GameSession).WithMany(s => s.GameMoves).HasForeignKey(m => m.GameSessionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(m => m.Player).WithMany().HasForeignKey(m => m.PlayerId).OnDelete(DeleteBehavior.Restrict);
    }
}
