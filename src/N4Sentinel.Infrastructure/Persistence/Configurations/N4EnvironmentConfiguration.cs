using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Configurations;

public class N4EnvironmentConfiguration : IEntityTypeConfiguration<N4Environment>
{
    public void Configure(EntityTypeBuilder<N4Environment> builder)
    {
        builder.ToTable("Environments");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Code).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.Kind).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(e => e.Code).IsUnique();

        builder.HasMany(e => e.Components)
            .WithOne(c => c.Environment)
            .HasForeignKey(c => c.EnvironmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Metadata.FindNavigation(nameof(N4Environment.Components))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
