using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Configurations;

public class SopExecutionConfiguration : IEntityTypeConfiguration<SopExecution>
{
    public void Configure(EntityTypeBuilder<SopExecution> builder)
    {
        builder.ToTable("SopExecutions");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.StartedByUserId).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.AbortReason).HasMaxLength(2000);

        builder.HasIndex(e => e.SopId);

        builder.HasMany(e => e.StepConfirmations)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(SopExecution.StepConfirmations))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
