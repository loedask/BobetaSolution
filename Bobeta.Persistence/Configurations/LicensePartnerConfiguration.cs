using Bobeta.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bobeta.Persistence.Configurations;

public class LicensePartnerConfiguration : IEntityTypeConfiguration<LicensePartner>
{
  public void Configure(EntityTypeBuilder<LicensePartner> builder)
  {
    builder.ToTable("LicensePartners");
    builder.HasKey(p => p.Id);
    builder.Property(p => p.LegalName).HasMaxLength(200).IsRequired();
    builder.Property(p => p.ContactEmail).HasMaxLength(256).IsRequired();
    builder.HasIndex(p => p.PortalUserId).IsUnique();
    builder.HasOne(p => p.PortalUser)
      .WithOne(u => u.LicensePartner)
      .HasForeignKey<LicensePartner>(p => p.PortalUserId)
      .OnDelete(DeleteBehavior.Cascade);
  }
}
