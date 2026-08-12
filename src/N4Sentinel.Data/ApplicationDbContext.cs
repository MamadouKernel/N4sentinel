using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using N4Sentinel.Data.Identite;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Data;

/// <summary>
/// Contexte applicatif. Au Sprint 1, il porte l'identité, les habilitations par environnement
/// et la piste d'audit. Les entités du référentiel arrivent au Sprint 2 ; <see cref="Environnements"/>
/// est déjà présent parce qu'une habilitation par environnement n'a pas de sens sans environnement.
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<UtilisateurApplicatif>(options)
{
    public DbSet<AuditEntry> EntreesDAudit => Set<AuditEntry>();

    public DbSet<HabilitationEnvironnement> Habilitations => Set<HabilitationEnvironnement>();

    public DbSet<N4Environment> Environnements => Set<N4Environment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AuditEntry>(entree =>
        {
            entree.ToTable("JournalDAudit");
            entree.Property(e => e.Acteur).HasMaxLength(256).IsRequired();
            entree.Property(e => e.Action).HasMaxLength(128).IsRequired();
            entree.Property(e => e.TypeDObjet).HasMaxLength(128).IsRequired();
            entree.Property(e => e.IdentifiantDObjet).HasMaxLength(256);
            entree.Property(e => e.AdresseIp).HasMaxLength(64);
            entree.Property(e => e.ReferenceDeCorrelation).HasMaxLength(128);

            // FR-091 — les journaux sont interrogés par période et par acteur.
            entree.HasIndex(e => e.SurvenueLe);
            entree.HasIndex(e => new { e.Acteur, e.SurvenueLe });
        });

        builder.Entity<HabilitationEnvironnement>(habilitation =>
        {
            habilitation.ToTable("HabilitationsParEnvironnement");
            habilitation.Property(h => h.UtilisateurId).HasMaxLength(450).IsRequired();
            habilitation.Property(h => h.AccordeePar).HasMaxLength(256).IsRequired();
            habilitation.Property(h => h.RevoqueePar).HasMaxLength(256);
            habilitation.HasIndex(h => new { h.UtilisateurId, h.EnvironmentId });
        });

        builder.Entity<N4Environment>(environnement =>
        {
            environnement.ToTable("Environnements");
            environnement.Property(e => e.Nom).HasMaxLength(128).IsRequired();
            environnement.Property(e => e.FuseauHoraire).HasMaxLength(64);
            environnement.HasIndex(e => e.Nom).IsUnique();
            environnement.HasMany(e => e.Responsables)
                .WithOne()
                .HasForeignKey(r => r.EnvironmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Les composants relèvent du Sprint 2 : ignorés tant que le référentiel n'existe pas.
            environnement.Ignore(e => e.Composants);
        });

        builder.Entity<EnvironmentResponsible>(responsable =>
        {
            responsable.ToTable("ResponsablesDEnvironnement");
            responsable.Property(r => r.Nom).HasMaxLength(256).IsRequired();
            responsable.Property(r => r.Role).HasMaxLength(128).IsRequired();
            responsable.Property(r => r.Email).HasMaxLength(256);
            responsable.Property(r => r.Telephone).HasMaxLength(64);
        });
    }
}
