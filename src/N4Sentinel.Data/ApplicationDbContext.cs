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

    public DbSet<OperationExecution> Executions => Set<OperationExecution>();

    public DbSet<VerrouDOperation> VerrousDOperation => Set<VerrouDOperation>();

    public DbSet<Workflow> Workflows => Set<Workflow>();

    public DbSet<WorkflowVersion> VersionsDeWorkflow => Set<WorkflowVersion>();

    public DbSet<ExecutionApproval> Approbations => Set<ExecutionApproval>();

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

        builder.Entity<OperationExecution>(execution =>
        {
            execution.ToTable("Executions");
            execution.Property(e => e.Reference).HasMaxLength(64).IsRequired();
            execution.Property(e => e.DemandePar).HasMaxLength(256).IsRequired();
            execution.Property(e => e.Motif).HasMaxLength(1024).IsRequired();
            execution.Property(e => e.ReferenceTicket).HasMaxLength(128);
            execution.Property(e => e.ApprouvePar).HasMaxLength(256);
            execution.Property(e => e.Resultat).HasMaxLength(2048);
            execution.Property(e => e.ReferenceDeCorrelation).HasMaxLength(128).IsRequired();
            execution.Property(e => e.Perimetre).HasMaxLength(1024);
            execution.Property(e => e.ImpactAttendu).HasMaxLength(1024);

            execution.HasIndex(e => e.Reference).IsUnique();
            execution.HasIndex(e => new { e.EnvironmentId, e.Statut });

            execution.HasMany(e => e.Etapes)
                .WithOne()
                .HasForeignKey(s => s.ExecutionId)
                .OnDelete(DeleteBehavior.Cascade);

            execution.HasMany(e => e.Approbations)
                .WithOne()
                .HasForeignKey(a => a.ExecutionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ExecutionApproval>(approbation =>
        {
            approbation.ToTable("ApprobationsDExecution");
            approbation.Property(a => a.ApprouvePar).HasMaxLength(256).IsRequired();
            approbation.Property(a => a.Motif).HasMaxLength(1024);

            // Un même acteur ne compte qu'une fois dans un circuit : la lecture des
            // approbations distinctes s'appuie sur cette unicité plutôt que de la recalculer.
            approbation.HasIndex(a => new { a.ExecutionId, a.ApprouvePar }).IsUnique();
        });

        builder.Entity<Workflow>(workflow =>
        {
            workflow.ToTable("Workflows");
            workflow.Property(w => w.Nom).HasMaxLength(128).IsRequired();
            workflow.Property(w => w.Description).HasMaxLength(1024);

            workflow.HasIndex(w => new { w.EnvironmentId, w.Nom }).IsUnique();

            workflow.HasMany(w => w.Versions)
                .WithOne()
                .HasForeignKey(v => v.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<WorkflowVersion>(version =>
        {
            version.ToTable("VersionsDeWorkflow");
            version.Property(v => v.CommentaireDeVersion).HasMaxLength(1024);
            version.Property(v => v.CreePar).HasMaxLength(256).IsRequired();
            version.Property(v => v.ValidePar).HasMaxLength(256);

            // Deux versions d'un même workflow ne peuvent pas porter le même numéro : la
            // version exécutable doit être désignée sans ambiguïté.
            version.HasIndex(v => new { v.WorkflowId, v.NumeroDeVersion }).IsUnique();

            version.HasMany(v => v.Etapes)
                .WithOne()
                .HasForeignKey(s => s.WorkflowVersionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<WorkflowStepDefinition>(etape =>
        {
            etape.ToTable("EtapesDeWorkflow");
            etape.Property(s => s.Libelle).HasMaxLength(256).IsRequired();
            etape.Property(s => s.Action).HasMaxLength(256).IsRequired();
            etape.Property(s => s.Condition).HasMaxLength(1024);

            etape.HasIndex(s => new { s.WorkflowVersionId, s.Ordre });
        });

        builder.Entity<ExecutionStep>(etape =>
        {
            etape.ToTable("EtapesDExecution");
            etape.Property(s => s.Libelle).HasMaxLength(256).IsRequired();
            etape.Property(s => s.Action).HasMaxLength(256).IsRequired();
            etape.Property(s => s.Preuve).HasMaxLength(4000);
            etape.Property(s => s.MessageDErreur).HasMaxLength(2048);
            etape.Property(s => s.Decision).HasMaxLength(512);
            etape.Property(s => s.DecidePar).HasMaxLength(256);
            etape.Property(s => s.OperateurExecutant).HasMaxLength(256);

            // La durée est calculée : elle ne se persiste pas, elle se déduit.
            etape.Ignore(s => s.Duree);

            etape.HasIndex(s => new { s.ExecutionId, s.Ordre });
        });

        builder.Entity<VerrouDOperation>(verrou =>
        {
            verrou.ToTable("VerrousDOperation");
            verrou.Property(v => v.DetenuPar).HasMaxLength(256).IsRequired();
            verrou.Property(v => v.LiberePar).HasMaxLength(256);

            // FR-015 — la lecture du verrou actif d'un environnement doit être immédiate :
            // elle conditionne le démarrage de toute opération mutative.
            verrou.HasIndex(v => new { v.EnvironmentId, v.LibereLe, v.ExpireLe });
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
