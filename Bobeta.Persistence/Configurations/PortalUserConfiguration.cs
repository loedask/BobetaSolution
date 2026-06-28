using Bobeta.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bobeta.Persistence.Configurations;

public class PortalUserConfiguration : IEntityTypeConfiguration<PortalUser>
{
  public void Configure(EntityTypeBuilder<PortalUser> builder)
  {
    builder.ToTable("PortalUsers");
    builder.HasKey(u => u.Id);
    builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
    builder.Property(u => u.DisplayName).HasMaxLength(100).IsRequired();
    builder.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
    builder.HasIndex(u => u.Email).IsUnique();
    builder.HasOne(u => u.CreatedBy)
      .WithMany()
      .HasForeignKey(u => u.CreatedById)
      .OnDelete(DeleteBehavior.SetNull);
  }
}
