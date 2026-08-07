using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Configurations;

public class ImportedLogFileConfiguration : IEntityTypeConfiguration<ImportedLogFile>
{
    public void Configure(EntityTypeBuilder<ImportedLogFile> builder)
    {
        builder.ToTable("ImportedLogFiles");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();

        builder.Property(f => f.FileName).HasMaxLength(300).IsRequired();
        builder.Property(f => f.Source).HasMaxLength(200);
        builder.Property(f => f.CorrelationReference).HasMaxLength(200);
        builder.Property(f => f.ContentHash).HasMaxLength(64).IsRequired();
        builder.Property(f => f.AnalysisStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(f => f.DetectedSignatures).HasMaxLength(1000);
        builder.Property(f => f.Verdict).HasConversion<string>().HasMaxLength(30);

        builder.HasIndex(f => f.EnvironmentId);
        builder.HasIndex(f => f.CorrelationReference);
    }
}
