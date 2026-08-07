using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Configurations;

public class SopExecutionStepConfirmationConfiguration : IEntityTypeConfiguration<SopExecutionStepConfirmation>
{
    public void Configure(EntityTypeBuilder<SopExecutionStepConfirmation> builder)
    {
        builder.ToTable("SopExecutionStepConfirmations");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.StepText).IsRequired();
        builder.Property(s => s.ConfirmedByUserId).HasMaxLength(256).IsRequired();
        builder.Property(s => s.Proof).HasMaxLength(2000);
        builder.Property(s => s.DeviationComment).HasMaxLength(2000);
    }
}
