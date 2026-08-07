using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Configurations;

public class WorkflowSimulationStepResultConfiguration : IEntityTypeConfiguration<WorkflowSimulationStepResult>
{
    public void Configure(EntityTypeBuilder<WorkflowSimulationStepResult> builder)
    {
        builder.ToTable("WorkflowSimulationStepResults");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.ComponentName).HasMaxLength(200);
        builder.Property(s => s.BlockingReason).HasMaxLength(500);
        builder.Property(s => s.Action).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.ObservedHealth).HasConversion<string>().HasMaxLength(20);
    }
}
