using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Configurations;

public class ReconstitutionStepRecordConfiguration : IEntityTypeConfiguration<ReconstitutionStepRecord>
{
    public void Configure(EntityTypeBuilder<ReconstitutionStepRecord> builder)
    {
        builder.ToTable("ReconstitutionStepRecords");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Step).HasConversion<string>().HasMaxLength(30);
        builder.Property(s => s.ConfirmedByUserId).HasMaxLength(256).IsRequired();
        builder.Property(s => s.Evidence).HasMaxLength(2000);
    }
}
