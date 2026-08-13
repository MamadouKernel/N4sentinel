using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Audit;
using N4Sentinel.Application.Connecteurs;
using N4Sentinel.Application.Referentiel;
using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Referentiel;

namespace N4Sentinel.Data.Referentiel;

/// <summary>
/// Écrit au référentiel la topologie lue dans la configuration des scripts d'exploitation.
///
/// La lecture du fichier appartient au domaine — <see cref="LecteurDeTopologieN4"/> — et l'ordre
/// des séquences aussi. Cette classe ne décide de rien : elle rapproche ce qui a été lu de ce
/// que le référentiel contient déjà, et écrit la différence.
/// </summary>
public sealed class ImportDeTopologie(
    ApplicationDbContext contexte,
    IUtilisateurCourant acteur,
    IClock horloge,
    IAuditTrail piste) : IImportDeTopologie
{
    public async Task<RapportDImport> ImporterAsync(
        Guid environnementId,
        ConfigurationN4 configuration,
        bool genererLesSequences,
        CancellationToken cancellationToken = default)
    {
        var environnement = await contexte.Environnements
            .FirstOrDefaultAsync(e => e.Id == environnementId, cancellationToken);

        if (environnement is null)
        {
            return Refuse("Environnement introuvable.");
        }

        var lecture = LecteurDeTopologieN4.Lire(configuration);

        if (!lecture.Exploitable)
        {
            await TracerLeRefusAsync(environnementId, "Topologie illisible ou vide.", cancellationToken);
            return Refuse("La configuration ne décrit aucun composant exploitable.") with
            {
                Anomalies = lecture.Anomalies
            };
        }

        var existants = await contexte.Composants
            .Where(c => c.EnvironmentId == environnementId)
            .ToListAsync(cancellationToken);

        var (crees, misAJour, inchanges, parNom) =
            Rapprocher(environnementId, lecture.Composants, existants);

        var workflows = genererLesSequences
            ? await GenererLesSequencesAsync(environnement, lecture.Composants, parNom, cancellationToken)
            : [];

        await contexte.SaveChangesAsync(cancellationToken);

        await piste.EnregistrerAsync(new AuditEntry
        {
            Acteur = acteur.NomAffiche,
            Action = ActionsAuditees.TopologieImportee,
            TypeDObjet = ObjetsAudites.Composant,
            IdentifiantDObjet = environnementId.ToString(),
            EnvironmentId = environnementId,
            ValeurAvant = $"{existants.Count} composant(s) au référentiel",
            // Les anomalies partent au journal : le fichier n'est pas conservé, et sans elles
            // personne ne pourrait plus dire, trois mois après, ce que l'import avait écarté.
            ValeurApres = $"{crees} créé(s), {misAJour} mis à jour, {inchanges} inchangé(s)"
                          + (workflows.Count > 0 ? $" ; {workflows.Count} séquence(s) générée(s)" : string.Empty)
                          + (lecture.Anomalies.Count > 0
                              ? $" ; écarté : {string.Join(" | ", lecture.Anomalies)}"
                              : " ; aucune anomalie"),
            AdresseIp = acteur.AdresseIp,
            Origine = AuditOrigin.InterfaceWeb
        }, cancellationToken);

        return new RapportDImport(
            crees, misAJour, inchanges, workflows, lecture.Anomalies, Applique: true,
            $"Topologie reprise sur {environnement.Nom}. "
            + "Composants et séquences créés en brouillon : leur activation reste un geste de validation.");
    }

    /// <summary>
    /// Rapproche par le nom : c'est l'identifiant stable d'un composant dans un environnement,
    /// et le référentiel le contraint déjà à l'unicité. Rapprocher par serveur confondrait deux
    /// rôles hébergés sur la même machine — Bridge et XPS le sont couramment.
    /// </summary>
    private (int Crees, int MisAJour, int Inchanges, Dictionary<string, N4Component> ParNom) Rapprocher(
        Guid environnementId,
        IReadOnlyList<ComposantDeTopologie> lus,
        List<N4Component> existants)
    {
        var parNom = existants.ToDictionary(c => c.Nom, StringComparer.OrdinalIgnoreCase);
        int crees = 0, misAJour = 0, inchanges = 0;

        foreach (var lu in lus)
        {
            if (!parNom.TryGetValue(lu.Nom, out var existant))
            {
                var nouveau = new N4Component
                {
                    EnvironmentId = environnementId,
                    Nom = lu.Nom,
                    Role = lu.Kind.ToString(),
                    Serveur = lu.Serveur,
                    NomDuService = lu.NomDuService,
                    Kind = lu.Kind,
                    ModeDePilotage = lu.ModeDePilotage,
                    Criticite = lu.Criticite,
                    // Brouillon : un fichier de configuration n'active pas un composant.
                    Statut = ValidationStatus.Brouillon
                };

                contexte.Composants.Add(nouveau);

                // Conservé dans la table de rapprochement : les étapes générées dans la même
                // unité de travail doivent pouvoir viser ce composant, qui n'est pas encore en
                // base et qu'aucune requête ne retrouverait.
                parNom[lu.Nom] = nouveau;
                crees++;
                continue;
            }

            var change = existant.Serveur != lu.Serveur
                         || existant.NomDuService != lu.NomDuService
                         || existant.Kind != lu.Kind;

            if (!change)
            {
                inchanges++;
                continue;
            }

            // Seule la fiche technique est reprise. Le mode de pilotage, la criticité et le
            // statut de validation ont pu être ajustés au référentiel en connaissance de cause :
            // un import ne les écrase pas.
            existant.Serveur = lu.Serveur;
            existant.NomDuService = lu.NomDuService;
            existant.Kind = lu.Kind;

            misAJour++;
        }

        return (crees, misAJour, inchanges, parNom);
    }

    private async Task<IReadOnlyList<string>> GenererLesSequencesAsync(
        N4Environment environnement,
        IReadOnlyList<ComposantDeTopologie> composants,
        Dictionary<string, N4Component> parNom,
        CancellationToken cancellationToken)
    {
        var generes = new List<string>();

        var arret = GenerateurDeSequenceDArret.Generer(composants);
        if (arret.Count > 0)
        {
            await CreerLaVersionAsync(
                environnement, WorkflowType.ArretComplet, "Arrêt complet (importé)",
                arret, ActionsDePilotage.ArreterServiceWindows, parNom, cancellationToken);
            generes.Add($"Arrêt complet — {arret.Count} étape(s)");
        }

        var demarrage = GenerateurDeSequenceDeDemarrage.Generer(composants);
        if (demarrage.Count > 0)
        {
            await CreerLaVersionAsync(
                environnement, WorkflowType.DemarrageComplet, "Démarrage complet (importé)",
                demarrage, ActionsDePilotage.DemarrerServiceWindows, parNom, cancellationToken);
            generes.Add($"Démarrage complet — {demarrage.Count} étape(s)");
        }

        return generes;
    }

    /// <summary>
    /// Une nouvelle version à chaque import, jamais une modification de l'existante : une
    /// version validée est figée (Sprint 6), et l'historique des séquences successives est
    /// précisément ce qui permet de dire ce qui a été exécuté le jour J.
    /// </summary>
    private async Task CreerLaVersionAsync(
        N4Environment environnement,
        WorkflowType type,
        string nom,
        IReadOnlyList<EtapeDArretPlanifiee> etapes,
        string action,
        Dictionary<string, N4Component> parNom,
        CancellationToken cancellationToken)
    {
        var workflow = await contexte.Workflows
            .Include(w => w.Versions)
            .FirstOrDefaultAsync(
                w => w.EnvironmentId == environnement.Id && w.Type == type, cancellationToken);

        if (workflow is null)
        {
            workflow = new Workflow
            {
                EnvironmentId = environnement.Id,
                Nom = nom,
                Type = type,
                Description = "Séquence dérivée de la configuration des scripts d'exploitation (SOP-2)."
            };

            contexte.Workflows.Add(workflow);
        }

        var version = new WorkflowVersion
        {
            WorkflowId = workflow.Id,
            NumeroDeVersion = workflow.Versions.Count == 0
                ? 1
                : workflow.Versions.Max(v => v.NumeroDeVersion) + 1,
            Statut = ValidationStatus.Brouillon,
            CreePar = acteur.NomAffiche,
            CreeLe = horloge.MaintenantUtc,
            CommentaireDeVersion = "Générée par import de topologie.",
            // Un arrêt complet touche tout l'écosystème : le droit ordinaire ne suffit pas.
            ActionSensible = type == WorkflowType.ArretComplet
        };

        foreach (var etape in etapes)
        {
            version.Etapes.Add(new WorkflowStepDefinition
            {
                WorkflowVersionId = version.Id,
                Ordre = etape.Ordre,
                Libelle = $"{Verbe(type)} {etape.ComposantNom}",
                Action = action,
                // Table de rapprochement plutôt que requête : les composants créés dans la même
                // unité de travail ne sont pas encore en base, et aucune requête ne les
                // retrouverait — l'étape générée viserait alors le vide.
                ComposantCibleId = parNom.TryGetValue(etape.ComposantNom, out var composant)
                    ? composant.Id
                    : null,
                TimeoutSecondes = etape.TimeoutSecondes
            });
        }

        workflow.Versions.Add(version);
    }

    private static string Verbe(WorkflowType type) =>
        type == WorkflowType.ArretComplet ? "Arrêter" : "Démarrer";

    private Task TracerLeRefusAsync(Guid environnementId, string motif, CancellationToken cancellationToken) =>
        piste.EnregistrerAsync(new AuditEntry
        {
            Acteur = acteur.NomAffiche,
            Action = ActionsAuditees.ModificationRefusee,
            TypeDObjet = ObjetsAudites.Composant,
            IdentifiantDObjet = environnementId.ToString(),
            AdresseIp = acteur.AdresseIp,
            Autorisee = false,
            MotifDeRefus = motif,
            Origine = AuditOrigin.InterfaceWeb
        }, cancellationToken);

    private static RapportDImport Refuse(string motif) =>
        new(0, 0, 0, [], [], Applique: false, motif);
}
