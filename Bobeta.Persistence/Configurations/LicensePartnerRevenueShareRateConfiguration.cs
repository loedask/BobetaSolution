using Bobeta.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bobeta.Persistence.Configurations;

public class LicensePartnerRevenueShareRateConfiguration : IEntityTypeConfiguration<LicensePartnerRevenueShareRate>
{
  public void Configure(EntityTypeBuilder<LicensePartnerRevenueShareRate> builder)
  {
    builder.ToTable("LicensePartnerRevenueShareRates");
    builder.HasKey(r => r.Id);
    builder.Property(r => r.RevenueSharePercent).HasPrecision(5, 2);
    builder.HasIndex(r => new { r.AssignmentId, r.EffectiveFrom });
    builder.HasOne(r => r.Assignment)
      .WithMany(a => a.RevenueShareRates)
      .HasForeignKey(r => r.AssignmentId)
      .OnDelete(DeleteBehavior.Cascade);
  }
}
