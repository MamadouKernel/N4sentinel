using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Configurations;

public class DiagnosticCaseConfiguration : IEntityTypeConfiguration<DiagnosticCase>
{
    public void Configure(EntityTypeBuilder<DiagnosticCase> builder)
    {
        builder.ToTable("DiagnosticCases");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Symptom).HasMaxLength(2000).IsRequired();
        builder.Property(c => c.CorrelationReference).HasMaxLength(200).IsRequired();
        builder.Property(c => c.RequestedByUserId).HasMaxLength(256).IsRequired();
        builder.Property(c => c.ConclusionLevel).HasConversion<string>().HasMaxLength(30);
        builder.Property(c => c.ConclusionSummary).HasMaxLength(4000);

        builder.HasIndex(c => c.EnvironmentId);
        builder.HasIndex(c => c.CorrelationReference);

        builder.HasMany(c => c.Hypotheses)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(DiagnosticCase.Hypotheses))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
