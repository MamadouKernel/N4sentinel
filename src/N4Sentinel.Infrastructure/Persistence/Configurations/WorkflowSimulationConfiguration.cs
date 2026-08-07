using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Configurations;

public class WorkflowSimulationConfiguration : IEntityTypeConfiguration<WorkflowSimulation>
{
    public void Configure(EntityTypeBuilder<WorkflowSimulation> builder)
    {
        builder.ToTable("WorkflowSimulations");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.RequestedByUserId).HasMaxLength(256).IsRequired();

        builder.HasIndex(s => s.WorkflowId);

        builder.HasMany(s => s.StepResults)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(WorkflowSimulation.StepResults))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
