using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Configurations;

public class HealthyReferencePeriodConfiguration : IEntityTypeConfiguration<HealthyReferencePeriod>
{
    public void Configure(EntityTypeBuilder<HealthyReferencePeriod> builder)
    {
        builder.ToTable("HealthyReferencePeriods");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Label).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Notes).HasMaxLength(2000);
        builder.Property(p => p.ValidatedByUserId).HasMaxLength(256).IsRequired();

        builder.HasIndex(p => p.EnvironmentId);
    }
}
