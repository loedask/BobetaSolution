using Bobeta.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bobeta.Persistence.Configurations;

public class OtpCodeConfiguration : IEntityTypeConfiguration<OtpCode>
{
    public void Configure(EntityTypeBuilder<OtpCode> builder)
    {
        builder.ToTable("OtpCodes");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.PhoneNumber).HasMaxLength(20).IsRequired();
        builder.Property(o => o.Code).HasMaxLength(10).IsRequired();
        builder.HasIndex(o => new { o.PhoneNumber, o.CreatedAt });
    }
}
