using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Configurations;

public class EdiFileConfiguration : IEntityTypeConfiguration<EdiFile>
{
    public void Configure(EntityTypeBuilder<EdiFile> builder)
    {
        builder.ToTable("EdiFiles");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();

        builder.Property(f => f.MessageType).HasMaxLength(100).IsRequired();
        builder.Property(f => f.PartnerName).HasMaxLength(200).IsRequired();
        builder.Property(f => f.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(f => f.LastErrorMessage).HasMaxLength(1000);

        builder.HasIndex(f => f.EnvironmentId);
    }
}
