using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Configurations;

public class AssistantFeedbackConfiguration : IEntityTypeConfiguration<AssistantFeedback>
{
    public void Configure(EntityTypeBuilder<AssistantFeedback> builder)
    {
        builder.ToTable("AssistantFeedbackEntries");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();

        builder.Property(f => f.QuestionText).HasMaxLength(1000).IsRequired();
        builder.Property(f => f.FlaggedExcerpt).HasMaxLength(2000);
        builder.Property(f => f.ProposedCorrection).HasMaxLength(2000).IsRequired();
        builder.Property(f => f.SubmittedByUserId).HasMaxLength(256).IsRequired();
        builder.Property(f => f.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(f => f.ReviewedByUserId).HasMaxLength(256);
        builder.Property(f => f.ReviewNotes).HasMaxLength(1000);

        builder.HasIndex(f => f.DocumentId);
    }
}
