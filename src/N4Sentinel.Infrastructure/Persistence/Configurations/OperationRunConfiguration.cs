using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Configurations;

public class OperationRunConfiguration : IEntityTypeConfiguration<OperationRun>
{
    public void Configure(EntityTypeBuilder<OperationRun> builder)
    {
        builder.ToTable("OperationRuns");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Motif).HasMaxLength(2000);
        builder.Property(r => r.InterventionWindowDescription).HasMaxLength(500);
        builder.Property(r => r.Impact).HasMaxLength(2000);
        builder.Property(r => r.IncidentOrChangeReference).HasMaxLength(200);
        builder.Property(r => r.RequestedByUserId).HasMaxLength(256).IsRequired();
        builder.Property(r => r.ApprovedByUserId).HasMaxLength(256);
        builder.Property(r => r.RejectionReason).HasMaxLength(1000);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(r => r.EnvironmentId);

        builder.HasMany(r => r.StepExecutions)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(OperationRun.StepExecutions))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
