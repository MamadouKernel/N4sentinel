using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Orchestration;
using N4Sentinel.Application.Supervision;
using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Orchestration;

namespace N4Sentinel.Orchestration;

/// <summary>
/// Machine à états persistante. Deux principes gouvernent l'implémentation :
///
/// **Rien n'est tenu en mémoire.** Chaque transition est écrite avant d'être considérée comme
/// acquise. Un moteur qui garderait son état en mémoire redémarrerait vierge après une panne,
/// et c'est précisément le scénario contre lequel ce sprint est écrit.
///
/// **Aucune reprise n'est aveugle.** Avant de repartir, l'état réel est recollecté et comparé
/// au mémorisé ; toute divergence bascule l'exécution en « Réconciliation requise » plutôt que
/// de rejouer une étape sur un système qui a changé entre-temps.
/// </summary>
public sealed class MoteurDOrchestration(
    IEtatDExecutionPersiste etat,
    IServiceDeSupervision supervision,
    IUtilisateurCourant acteur,
    IClock horloge) : IMoteurDOrchestration
{
    public async Task<ReponseDuMoteur> DemarrerAsync(
        Guid executionId,
        CancellationToken cancellationToken = default)
    {
        var execution = await etat.LireAsync(executionId, cancellationToken);
        if (execution is null)
        {
            return new ReponseDuMoteur(false, ExecutionStatus.EnPreparation, "Exécution introuvable.");
        }

        if (!MachineAEtats.EstAutorisee(execution.Statut, ExecutionStatus.EnCours))
        {
            return Refus(execution, $"Transition {execution.Statut} → EnCours interdite.");
        }

        // FR-015 — une seule opération mutative à la fois par environnement.
        var verrou = await etat.LireLeVerrouActifAsync(execution.EnvironmentId, cancellationToken);
        if (verrou is not null && verrou.ExecutionId != executionId)
        {
            return Refus(execution,
                $"Environnement déjà verrouillé. {verrou.Description(horloge.MaintenantUtc)}");
        }

        if (verrou is null)
        {
            await etat.PoserLeVerrouAsync(new VerrouDOperation
            {
                EnvironmentId = execution.EnvironmentId,
                ExecutionId = executionId,
                DetenuPar = acteur.NomAffiche,
                AcquisLe = horloge.MaintenantUtc,
                ExpireLe = horloge.MaintenantUtc.AddHours(4)
            }, cancellationToken);
        }

        execution.Statut = ExecutionStatus.EnCours;
        execution.DebutLe ??= horloge.MaintenantUtc;
        await etat.EnregistrerAsync(cancellationToken);

        return new ReponseDuMoteur(true, execution.Statut, "Exécution engagée.");
    }

    public async Task<ReponseDuMoteur> MettreEnPauseAsync(
        Guid executionId,
        CancellationToken cancellationToken = default)
    {
        var execution = await etat.LireAsync(executionId, cancellationToken);
        if (execution is null)
        {
            return new ReponseDuMoteur(false, ExecutionStatus.EnPreparation, "Exécution introuvable.");
        }

        if (!MachineAEtats.EstAutorisee(execution.Statut, ExecutionStatus.EnPause))
        {
            return Refus(execution, $"Une exécution {execution.Statut} ne peut pas être mise en pause.");
        }

        execution.Statut = ExecutionStatus.EnPause;
        await etat.EnregistrerAsync(cancellationToken);

        // FR-024 — la pause prend effet au prochain point sûr : une étape déjà engagée va
        // à son terme, on ne coupe pas une commande en vol.
        return new ReponseDuMoteur(true, execution.Statut,
            "Pause demandée : elle prendra effet au prochain point sûr. "
            + "Une étape déjà engagée n'est pas interrompue.");
    }

    public async Task<ReponseDuMoteur> ReprendreAsync(
        Guid executionId,
        CancellationToken cancellationToken = default)
    {
        var execution = await etat.LireAsync(executionId, cancellationToken);
        if (execution is null)
        {
            return new ReponseDuMoteur(false, ExecutionStatus.EnPreparation, "Exécution introuvable.");
        }

        if (!MachineAEtats.EstAutorisee(execution.Statut, ExecutionStatus.EnCours))
        {
            return Refus(execution, $"Une exécution {execution.Statut} ne peut pas être reprise.");
        }

        var verdict = await ControlerLEtatReelAsync(execution, cancellationToken);

        execution.Statut = verdict.StatutARetenir;
        await etat.EnregistrerAsync(cancellationToken);

        return new ReponseDuMoteur(verdict.RepriseAutorisee, execution.Statut, verdict.Motif);
    }

    public async Task<ReponseDuMoteur> DemanderLAnnulationAsync(
        Guid executionId,
        CancellationToken cancellationToken = default)
    {
        var execution = await etat.LireAsync(executionId, cancellationToken);
        if (execution is null)
        {
            return new ReponseDuMoteur(false, ExecutionStatus.EnPreparation, "Exécution introuvable.");
        }

        if (!MachineAEtats.EstAutorisee(execution.Statut, ExecutionStatus.AnnulationDemandee))
        {
            return Refus(execution, $"Une exécution {execution.Statut} ne peut pas être annulée.");
        }

        execution.Statut = ExecutionStatus.AnnulationDemandee;
        await etat.EnregistrerAsync(cancellationToken);

        // FR-025 — « une annulation n'interrompt jamais brutalement une commande engagée ».
        return new ReponseDuMoteur(true, execution.Statut,
            "Annulation demandée. Elle sera appliquée au prochain point sûr ; "
            + "aucune commande engagée n'est interrompue.");
    }

    public async Task<int> RecupererLesExecutionsInterrompuesAsync(
        CancellationToken cancellationToken = default)
    {
        var interrompues = await etat.LireLesExecutionsNonTermineesAsync(cancellationToken);
        var reprises = 0;

        foreach (var execution in interrompues.Where(e => e.Statut == ExecutionStatus.EnCours))
        {
            // Une exécution retrouvée « En cours » après un redémarrage n'était pas en cours :
            // le processus qui la portait n'existe plus. Elle est mise en pause, puis soumise
            // au contrôle de reprise comme n'importe quelle reprise manuelle.
            var verdict = await ControlerLEtatReelAsync(execution, cancellationToken);

            execution.Statut = verdict.RepriseAutorisee
                ? ExecutionStatus.EnPause
                : ExecutionStatus.ReconciliationRequise;

            execution.Resultat = verdict.RepriseAutorisee
                ? "Reprise après redémarrage du serveur applicatif : état réel conforme, "
                  + "exécution mise en pause dans l'attente d'une décision."
                : $"Reprise après redémarrage du serveur applicatif refusée. {verdict.Motif}";

            reprises++;
        }

        if (reprises > 0)
        {
            await etat.EnregistrerAsync(cancellationToken);
        }

        return reprises;
    }

    /// <summary>
    /// Recollecte l'état réel des composants visés par l'exécution et le compare au mémorisé.
    /// </summary>
    private async Task<VerdictDeReprise> ControlerLEtatReelAsync(
        OperationExecution execution,
        CancellationToken cancellationToken)
    {
        var cartographie = await supervision.LireAsync(execution.EnvironmentId, cancellationToken);
        if (cartographie is null)
        {
            return ControleDeReprise.Evaluer([]);
        }

        var composantsVises = execution.Etapes
            .Where(e => e.ComposantCibleId is not null)
            .Select(e => e.ComposantCibleId!.Value)
            .Distinct()
            .ToHashSet();

        var comparaisons = cartographie.Lignes
            .Where(l => composantsVises.Count == 0 || composantsVises.Contains(l.ComposantId))
            .Select(l => new ComparaisonDEtat(
                l.ComposantId,
                l.Nom,
                EtatMemorise(execution, l.ComposantId),
                Convertir(l.Etat.Etat)))
            .ToList();

        return ControleDeReprise.Evaluer(comparaisons);
    }

    /// <summary>
    /// État que le moteur avait retenu pour ce composant : celui de la dernière étape close
    /// qui le visait. Sans étape close, on n'a rien mémorisé — et on le dit.
    /// </summary>
    private static ComponentHealth EtatMemorise(OperationExecution execution, Guid composantId)
    {
        var derniere = execution.Etapes
            .Where(e => e.ComposantCibleId == composantId && e.FinLe is not null)
            .OrderByDescending(e => e.FinLe)
            .FirstOrDefault();

        return derniere?.Statut switch
        {
            StepStatus.Reussi => ComponentHealth.Operationnel,
            StepStatus.Avertissement => ComponentHealth.Degrade,
            StepStatus.Echec => ComponentHealth.Arrete,
            _ => ComponentHealth.Inconnu
        };
    }

    private static ComponentHealth Convertir(Domain.Supervision.EtatDeSupervision etat) => etat switch
    {
        Domain.Supervision.EtatDeSupervision.Disponible => ComponentHealth.Operationnel,
        Domain.Supervision.EtatDeSupervision.Degrade => ComponentHealth.Degrade,
        Domain.Supervision.EtatDeSupervision.Indisponible => ComponentHealth.Arrete,
        Domain.Supervision.EtatDeSupervision.AConfirmer => ComponentHealth.AConfirmer,
        _ => ComponentHealth.Inconnu
    };

    private static ReponseDuMoteur Refus(OperationExecution execution, string motif) =>
        new(false, execution.Statut, motif);
}
