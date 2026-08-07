using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Configurations;

public class DiagnosticHypothesisConfiguration : IEntityTypeConfiguration<DiagnosticHypothesis>
{
    public void Configure(EntityTypeBuilder<DiagnosticHypothesis> builder)
    {
        builder.ToTable("DiagnosticHypotheses");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();

        builder.Property(h => h.Domain).HasConversion<string>().HasMaxLength(30);
        builder.Property(h => h.AppliedRuleKey).HasMaxLength(100);
        builder.Property(h => h.CauseDescription).HasMaxLength(1000).IsRequired();
        builder.Property(h => h.ConfidenceLevel).HasConversion<string>().HasMaxLength(20);
        builder.Property(h => h.SupportingEvidence).HasMaxLength(2000);
        builder.Property(h => h.ContradictingEvidence).HasMaxLength(2000);
        builder.Property(h => h.MissingInformation).HasMaxLength(2000);
        builder.Property(h => h.RecommendedChecks).HasMaxLength(2000);
        builder.Property(h => h.SafeActionsOrEscalation).HasMaxLength(2000);
    }
}
