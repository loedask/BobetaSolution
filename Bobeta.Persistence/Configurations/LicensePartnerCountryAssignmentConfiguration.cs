using Bobeta.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bobeta.Persistence.Configurations;

public class LicensePartnerCountryAssignmentConfiguration : IEntityTypeConfiguration<LicensePartnerCountryAssignment>
{
  public void Configure(EntityTypeBuilder<LicensePartnerCountryAssignment> builder)
  {
    builder.ToTable("LicensePartnerCountryAssignments");
    builder.HasKey(a => a.Id);
    builder.Property(a => a.CountryCode).HasMaxLength(2).IsRequired();
    builder.HasIndex(a => new { a.CountryCode, a.IsActive });
    builder.HasIndex(a => new { a.LicensePartnerId, a.CountryCode }).IsUnique();
    builder.HasOne(a => a.LicensePartner)
      .WithMany(p => p.CountryAssignments)
      .HasForeignKey(a => a.LicensePartnerId)
      .OnDelete(DeleteBehavior.Cascade);
  }
}
