using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using N4Sentinel.Data.Identite;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Data;

/// <summary>
/// Contexte applicatif : identité, habilitations par environnement, piste d'audit (Sprint 1)
/// et référentiel — environnements, composants, endpoints, contrôles et dépendances (Sprint 2).
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<UtilisateurApplicatif>(options)
{
    public DbSet<AuditEntry> EntreesDAudit => Set<AuditEntry>();

    public DbSet<HabilitationEnvironnement> Habilitations => Set<HabilitationEnvironnement>();

    public DbSet<N4Environment> Environnements => Set<N4Environment>();

    public DbSet<N4Component> Composants => Set<N4Component>();

    public DbSet<ComponentDependency> Dependances => Set<ComponentDependency>();

    public DbSet<ControlSignal> Releves => Set<ControlSignal>();

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

            environnement.HasMany(e => e.Composants)
                .WithOne()
                .HasForeignKey(c => c.EnvironmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<N4Component>(composant =>
        {
            composant.ToTable("Composants");
            composant.Property(c => c.Nom).HasMaxLength(128).IsRequired();
            composant.Property(c => c.Role).HasMaxLength(256).IsRequired();
            composant.Property(c => c.Serveur).HasMaxLength(256).IsRequired();
            composant.Property(c => c.AdresseIp).HasMaxLength(64);
            composant.Property(c => c.NomDns).HasMaxLength(256);
            composant.Property(c => c.SystemeDExploitation).HasMaxLength(128);
            composant.Property(c => c.NomDuService).HasMaxLength(128);
            composant.Property(c => c.Mecanisme).HasMaxLength(256);
            composant.Property(c => c.Responsable).HasMaxLength(256);

            // Deux composants ne peuvent pas porter le même nom dans un même environnement :
            // une opération viserait alors une cible ambiguë.
            composant.HasIndex(c => new { c.EnvironmentId, c.Nom }).IsUnique();

            composant.HasMany(c => c.Endpoints)
                .WithOne()
                .HasForeignKey(e => e.ComponentId)
                .OnDelete(DeleteBehavior.Cascade);

            composant.HasMany(c => c.Controles)
                .WithOne()
                .HasForeignKey(v => v.ComponentId)
                .OnDelete(DeleteBehavior.Cascade);

            composant.HasMany(c => c.Dependances)
                .WithOne()
                .HasForeignKey(d => d.ComponentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ComponentEndpoint>(point =>
        {
            point.ToTable("ComposantEndpoints");
            point.Property(e => e.Libelle).HasMaxLength(128).IsRequired();
            point.Property(e => e.Protocole).HasMaxLength(32).IsRequired();
            point.Property(e => e.Hote).HasMaxLength(256).IsRequired();
            point.Property(e => e.Chemin).HasMaxLength(512);
        });

        builder.Entity<ComponentCheck>(controle =>
        {
            controle.ToTable("ComposantControles");
            controle.Property(v => v.Libelle).HasMaxLength(128).IsRequired();
            controle.Property(v => v.TypeDeControle).HasMaxLength(64).IsRequired();
            controle.Property(v => v.Parametres).HasMaxLength(1024);
        });

        builder.Entity<ComponentDependency>(dependance =>
        {
            dependance.ToTable("ComposantDependances");
            dependance.Property(d => d.Justification).HasMaxLength(512);

            // Le prérequis n'est pas déclaré en clé étrangère : la suppression en cascade
            // effacerait des dépendances sans que personne le voie. Elles sont retirées
            // explicitement, et l'opération est auditée.
            dependance.HasIndex(d => new { d.ComponentId, d.ComposantRequisId }).IsUnique();
        });

        builder.Entity<ControlSignal>(releve =>
        {
            releve.ToTable("Releves");
            releve.Property(r => r.Type).HasMaxLength(64).IsRequired();
            releve.Property(r => r.Cible).HasMaxLength(512).IsRequired();
            releve.Property(r => r.Valeur).HasMaxLength(1024);
            releve.Property(r => r.Unite).HasMaxLength(32);
            releve.Property(r => r.SeuilAttendu).HasMaxLength(128);
            releve.Property(r => r.MotifIndisponibilite).HasMaxLength(1024);
            releve.Property(r => r.ReferenceDeCorrelation).HasMaxLength(128);

            // FR-053 — le tableau de bord lit le dernier relevé par composant et par type :
            // sans cet index, chaque rafraîchissement parcourrait tout l'historique.
            releve.HasIndex(r => new { r.ComposantCibleId, r.Type, r.ReleveLe });
            releve.HasIndex(r => r.ReleveLe);
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
