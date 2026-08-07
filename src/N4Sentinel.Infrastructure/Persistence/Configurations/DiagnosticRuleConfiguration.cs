using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Configurations;

public class DiagnosticRuleConfiguration : IEntityTypeConfiguration<DiagnosticRule>
{
    public void Configure(EntityTypeBuilder<DiagnosticRule> builder)
    {
        builder.ToTable("DiagnosticRules");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.RuleKey).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Domain).HasConversion<string>().HasMaxLength(30);
        builder.Property(r => r.ConditionDescription).HasMaxLength(2000).IsRequired();
        builder.Property(r => r.RequiredSources).HasMaxLength(1000).IsRequired();
        builder.Property(r => r.Hypothesis).HasMaxLength(1000).IsRequired();
        builder.Property(r => r.Severity).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.ConfidenceCalculationMethod).HasMaxLength(1000).IsRequired();
        builder.Property(r => r.AdditionalChecks).HasMaxLength(1000);
        builder.Property(r => r.Recommendation).HasMaxLength(2000).IsRequired();
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(r => new { r.RuleKey, r.VersionNumber }).IsUnique();
    }
}
