using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Configurations;

public class WorkflowStepConfiguration : IEntityTypeConfiguration<WorkflowStep>
{
    public void Configure(EntityTypeBuilder<WorkflowStep> builder)
    {
        builder.ToTable("WorkflowSteps");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.SuccessCriteria).HasMaxLength(2000);
        builder.Property(s => s.Action).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.OnFailurePolicy).HasConversion<string>().HasMaxLength(30);

        builder.Property(s => s.PrerequisiteStepIds)
            .HasField("_prerequisiteStepIds")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("PrerequisiteStepIds");
    }
}
