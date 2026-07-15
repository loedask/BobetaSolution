using Bobeta.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bobeta.Persistence.Configurations;

public class InfluencerCodeRedemptionConfiguration : IEntityTypeConfiguration<InfluencerCodeRedemption>
{
  public void Configure(EntityTypeBuilder<InfluencerCodeRedemption> builder)
  {
    builder.ToTable("InfluencerCodeRedemptions");
    builder.HasKey(r => r.Id);
    builder.Property(r => r.Code).HasMaxLength(32).IsRequired();
    builder.HasIndex(r => new { r.InfluencerId, r.PlayerId }).IsUnique();
    builder.HasIndex(r => new { r.PlayerId, r.GameSessionId });
    builder.HasOne(r => r.Influencer)
      .WithMany(i => i.Redemptions)
      .HasForeignKey(r => r.InfluencerId)
      .OnDelete(DeleteBehavior.Restrict);
    builder.HasOne(r => r.Player)
      .WithMany()
      .HasForeignKey(r => r.PlayerId)
      .OnDelete(DeleteBehavior.Restrict);
    builder.HasOne(r => r.GameSession)
      .WithMany()
      .HasForeignKey(r => r.GameSessionId)
      .OnDelete(DeleteBehavior.SetNull);
  }
}
