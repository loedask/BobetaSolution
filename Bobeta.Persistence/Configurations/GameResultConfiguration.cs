using Bobeta.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bobeta.Persistence.Configurations;

public class GameResultConfiguration : IEntityTypeConfiguration<GameResult>
{
    public void Configure(EntityTypeBuilder<GameResult> builder)
    {
        builder.ToTable("GameResults");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.TotalPot).HasPrecision(18, 2);
        builder.Property(r => r.WinnerAmount).HasPrecision(18, 2);
        builder.Property(r => r.PlatformCommission).HasPrecision(18, 2);
        builder.HasOne(r => r.GameSession).WithOne(s => s.GameResult).HasForeignKey<GameResult>(r => r.GameSessionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(r => r.WinnerPlayer).WithMany().HasForeignKey(r => r.WinnerPlayerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.LoserPlayer).WithMany().HasForeignKey(r => r.LoserPlayerId).OnDelete(DeleteBehavior.Restrict);
    }
}
