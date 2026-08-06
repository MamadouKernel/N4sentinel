using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Configurations;

public class WorkflowConfiguration : IEntityTypeConfiguration<Workflow>
{
    public void Configure(EntityTypeBuilder<Workflow> builder)
    {
        builder.ToTable("Workflows");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedNever();

        builder.Property(w => w.Name).HasMaxLength(200).IsRequired();
        builder.Property(w => w.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(w => w.Scope).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(w => new { w.EnvironmentId, w.Name }).IsUnique();

        builder.Property(w => w.TargetComponentIds)
            .HasField("_targetComponentIds")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("TargetComponentIds");

        builder.HasMany(w => w.Versions)
            .WithOne()
            .HasForeignKey(v => v.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Workflow.Versions))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
