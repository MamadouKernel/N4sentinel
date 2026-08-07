using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Configurations;

public class DiagnosticSignalConfiguration : IEntityTypeConfiguration<DiagnosticSignal>
{
    public void Configure(EntityTypeBuilder<DiagnosticSignal> builder)
    {
        builder.ToTable("DiagnosticSignals");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Domain).HasConversion<string>().HasMaxLength(30);
        builder.Property(s => s.Source).HasMaxLength(300).IsRequired();
        builder.Property(s => s.ComponentName).HasMaxLength(200);
        builder.Property(s => s.CorrelationReference).HasMaxLength(200).IsRequired();
        builder.Property(s => s.CollectionStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.UnavailableReason).HasConversion<string>().HasMaxLength(30);
        builder.Property(s => s.Content).HasMaxLength(8000);
        builder.Property(s => s.Reliability).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(s => s.EnvironmentId);
        builder.HasIndex(s => s.CorrelationReference);
    }
}
