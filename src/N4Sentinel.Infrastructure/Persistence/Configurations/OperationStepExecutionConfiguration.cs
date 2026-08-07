using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Configurations;

public class OperationStepExecutionConfiguration : IEntityTypeConfiguration<OperationStepExecution>
{
    public void Configure(EntityTypeBuilder<OperationStepExecution> builder)
    {
        builder.ToTable("OperationStepExecutions");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.ComponentName).HasMaxLength(200);
        builder.Property(s => s.ResultMessage).HasMaxLength(2000);
        builder.Property(s => s.Action).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
    }
}
