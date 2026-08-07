using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Configurations;

public class SopAssociationConfiguration : IEntityTypeConfiguration<SopAssociation>
{
    public void Configure(EntityTypeBuilder<SopAssociation> builder)
    {
        builder.ToTable("SopAssociations");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.ComponentName).HasMaxLength(200);
        builder.Property(a => a.ErrorMessage).HasMaxLength(2000);
        builder.Property(a => a.Result).HasMaxLength(2000);
        builder.Property(a => a.Evidence).HasMaxLength(2000);
        builder.Property(a => a.AttachedByUserId).HasMaxLength(256).IsRequired();

        builder.HasIndex(a => a.SopId);
        builder.HasIndex(a => a.DiagnosticCaseId);
        builder.HasIndex(a => a.OperationRunId);
    }
}
