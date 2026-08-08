using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Configurations;

public sealed class SequenceTemplateConfiguration : IEntityTypeConfiguration<SequenceTemplate>
{
    public void Configure(EntityTypeBuilder<SequenceTemplate> builder)
    {
        builder.ToTable("SequenceTemplates");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TemplateKey).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(2000);

        // Une seule ligne par (clé, version) : le versionnement se fait par nouvelle ligne, pas par écrasement.
        builder.HasIndex(x => new { x.TemplateKey, x.VersionNumber }).IsUnique();

        builder.HasMany(x => x.Tiers)
            .WithOne()
            .HasForeignKey(t => t.SequenceTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Tiers).Metadata.SetField("_tiers");
        builder.Navigation(x => x.Tiers).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class SequenceTierConfiguration : IEntityTypeConfiguration<SequenceTier>
{
    public void Configure(EntityTypeBuilder<SequenceTier> builder)
    {
        builder.ToTable("SequenceTiers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Label).IsRequired().HasMaxLength(200);
        builder.Property(x => x.SuccessCriteria).HasMaxLength(1000);
        builder.Property(x => x.SourceReference).HasMaxLength(200);

        builder.HasIndex(x => new { x.SequenceTemplateId, x.Position });
    }
}
