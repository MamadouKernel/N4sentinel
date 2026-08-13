using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Connecteurs;
using N4Sentinel.Application.Orchestration;
using N4Sentinel.Application.Supervision;
using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Execution;
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
///
/// Sprint 7 — l'exécution réelle. Les méthodes ci-dessous qui gardent une décision d'habilitation
/// (approbation, contournement) supposent que l'appelant a déjà vérifié le droit et la séparation
/// des responsabilités, exactement comme le fait déjà <c>PointsDEntreeDesOperations</c> pour
/// l'approbation d'exécution du Sprint 6 : le moteur enregistre une décision autorisée, il ne
/// l'autorise pas lui-même.
/// </summary>
public sealed class MoteurDOrchestration(
    IEtatDExecutionPersiste etat,
    IServiceDeSupervision supervision,
    IRepartiteurDeCommandes commandes,
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

        // Sprint 8 — « démarrage complet impossible si un composant reste actif ». Le contrôle
        // est ici, à l'engagement, et non à la première étape : refuser après avoir déjà
        // verrouillé l'environnement et démarré un nœud laisserait l'écosystème dans un état
        // pire que celui trouvé.
        var typeDOperation = await etat.LireLeTypeDuWorkflowAsync(
            execution.WorkflowVersionId, cancellationToken);

        if (typeDOperation == WorkflowType.DemarrageComplet)
        {
            var verdict = await VerifierQueRienNEstDeboutAsync(execution, cancellationToken);
            if (!verdict.Autorise)
            {
                return Refus(execution,
                    verdict.Motif + " À arrêter dans cet ordre : "
                    + string.Join(", ", verdict.ComposantsEnCause) + ".");
            }
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

    // — Sprint 7 : exécution réelle —

    public async Task<ReponseDuMoteur> AvancerAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var execution = await etat.LireAsync(executionId, cancellationToken);
        if (execution is null)
        {
            return new ReponseDuMoteur(false, ExecutionStatus.EnPreparation, "Exécution introuvable.");
        }

        if (execution.Statut != ExecutionStatus.EnCours)
        {
            return Refus(execution, $"Une exécution {execution.Statut} ne peut pas être avancée.");
        }

        var definitions = await etat.LireLesDefinitionsDEtapesAsync(execution.WorkflowVersionId, cancellationToken);
        var definitionsParId = definitions.ToDictionary(d => d.Id);

        // Une étape déjà « En cours » n'est jamais relancée : on relit son effet.
        var enCours = execution.Etapes.FirstOrDefault(e => e.Statut == StepStatus.EnCours);
        if (enCours is not null)
        {
            if (!definitionsParId.TryGetValue(enCours.WorkflowStepDefinitionId, out var definitionEnCours))
            {
                return Refus(execution, $"Définition de l'étape {enCours.Ordre} introuvable.");
            }

            return await VerifierLEffetEtConclureAsync(execution, enCours, definitionEnCours, cancellationToken);
        }

        var suivante = execution.Etapes
            .Where(e => e.Statut is StepStatus.AVenir or StepStatus.EnAttente)
            .OrderBy(e => e.Ordre)
            .FirstOrDefault();

        if (suivante is null)
        {
            return await ConclureSiPossibleAsync(execution, cancellationToken);
        }

        if (!definitionsParId.TryGetValue(suivante.WorkflowStepDefinitionId, out var definition))
        {
            return Refus(execution, $"Définition de l'étape {suivante.Ordre} introuvable.");
        }

        var autorisationObtenue = suivante.Decision is "Confirmée" or "Approuvée";
        var decisionDeLancement = PolitiqueDeLancement.Evaluer(
            definition.ConfirmationRequise, definition.ApprobationRequise, autorisationObtenue);

        if (!decisionDeLancement.Autorise)
        {
            suivante.Statut = StepStatus.EnAttente;
            suivante.MessageDErreur = decisionDeLancement.Motif;
            await etat.EnregistrerAsync(cancellationToken);
            return new ReponseDuMoteur(true, execution.Statut, decisionDeLancement.Motif);
        }

        return await LancerLEtapeAsync(execution, suivante, definition, cancellationToken);
    }

    public async Task<ReponseDuMoteur> ConfirmerLEtapeAsync(
        Guid executionId, Guid etapeId, CancellationToken cancellationToken = default)
    {
        var (execution, etape, definition, erreur) = await ChargerAsync(executionId, etapeId, cancellationToken);
        if (erreur is not null)
        {
            return erreur;
        }

        if (definition!.ApprobationRequise)
        {
            return Refus(execution!, $"Étape {etape!.Ordre} : exige une approbation, pas une simple confirmation.");
        }

        if (etape!.Statut != StepStatus.EnAttente)
        {
            return Refus(execution!, $"Étape {etape.Ordre} : rien à confirmer dans l'état {etape.Statut}.");
        }

        etape.Decision = "Confirmée";
        etape.DecidePar = acteur.NomAffiche;
        etape.DecideLe = horloge.MaintenantUtc;
        etape.Statut = StepStatus.AVenir;
        etape.MessageDErreur = null;
        await etat.EnregistrerAsync(cancellationToken);

        return new ReponseDuMoteur(true, execution!.Statut, $"Étape {etape.Ordre} confirmée par {acteur.NomAffiche}.");
    }

    public async Task<ReponseDuMoteur> ApprouverLEtapeAsync(
        Guid executionId, Guid etapeId, CancellationToken cancellationToken = default)
    {
        var (execution, etape, definition, erreur) = await ChargerAsync(executionId, etapeId, cancellationToken);
        if (erreur is not null)
        {
            return erreur;
        }

        if (!definition!.ApprobationRequise)
        {
            return Refus(execution!, $"Étape {etape!.Ordre} : n'exige pas d'approbation.");
        }

        if (etape!.Statut != StepStatus.EnAttente)
        {
            return Refus(execution!, $"Étape {etape.Ordre} : rien à approuver dans l'état {etape.Statut}.");
        }

        etape.Decision = "Approuvée";
        etape.DecidePar = acteur.NomAffiche;
        etape.DecideLe = horloge.MaintenantUtc;
        etape.Statut = StepStatus.AVenir;
        etape.MessageDErreur = null;
        await etat.EnregistrerAsync(cancellationToken);

        return new ReponseDuMoteur(true, execution!.Statut, $"Étape {etape.Ordre} approuvée par {acteur.NomAffiche}.");
    }

    public async Task<ReponseDuMoteur> DemanderUnContournementAsync(
        Guid executionId, Guid etapeId, string motif, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motif);

        var (execution, etape, definition, erreur) = await ChargerAsync(executionId, etapeId, cancellationToken);
        if (erreur is not null)
        {
            return erreur;
        }

        if (etape!.Statut != StepStatus.Bloque)
        {
            return Refus(execution!, $"Étape {etape.Ordre} : rien à contourner dans l'état {etape.Statut}.");
        }

        if (!definition!.Contournable)
        {
            return Refus(execution!,
                $"Étape {etape.Ordre} : le contrôle bloquant n'est pas déclaré contournable dans la version validée.");
        }

        etape.Decision = $"Contournement demandé : {motif}";
        etape.DecidePar = acteur.NomAffiche;
        etape.DecideLe = horloge.MaintenantUtc;
        await etat.EnregistrerAsync(cancellationToken);

        return new ReponseDuMoteur(true, execution!.Statut, $"Contournement demandé par {acteur.NomAffiche} : {motif}");
    }

    public async Task<ReponseDuMoteur> ApprouverLeContournementAsync(
        Guid executionId, Guid etapeId, CancellationToken cancellationToken = default)
    {
        var (execution, etape, _, erreur) = await ChargerAsync(executionId, etapeId, cancellationToken);
        if (erreur is not null)
        {
            return erreur;
        }

        if (etape!.Statut != StepStatus.Bloque
            || etape.Decision is null
            || !etape.Decision.StartsWith("Contournement demandé", StringComparison.Ordinal))
        {
            return Refus(execution!, $"Étape {etape.Ordre} : aucun contournement en attente d'approbation.");
        }

        etape.Statut = StepStatus.Ignore;
        etape.Decision = $"{etape.Decision} — approuvé par {acteur.NomAffiche}";
        etape.DecidePar = acteur.NomAffiche;
        etape.DecideLe = horloge.MaintenantUtc;
        etape.FinLe ??= horloge.MaintenantUtc;
        await etat.EnregistrerAsync(cancellationToken);

        return new ReponseDuMoteur(true, execution!.Statut,
            $"Contournement approuvé par {acteur.NomAffiche} : étape {etape.Ordre} ignorée.");
    }

    public async Task<ReponseDuMoteur> ForcerLArretAsync(
        Guid executionId, Guid etapeId, CancellationToken cancellationToken = default)
    {
        var (execution, etape, definition, erreur) = await ChargerAsync(executionId, etapeId, cancellationToken);
        if (erreur is not null)
        {
            return erreur;
        }

        if (etape!.Statut != StepStatus.EnCours)
        {
            return Refus(execution!, $"Étape {etape.Ordre} : rien à forcer dans l'état {etape.Statut}.");
        }

        // « Arrêt forcé proposé seulement après délai » (plan, S7) : un service bloqué en
        // Stopping s'arrête très souvent de lui-même. Le refus est ici, côté moteur, et pas
        // seulement dans la visibilité du bouton — un POST direct doit se heurter à la règle.
        var escalade = PolitiqueDEscalade.EvaluerLArretForce(
            etape.DebutLe, horloge.MaintenantUtc, definition!.TimeoutSecondes);

        if (!escalade.ArretForceOuvert)
        {
            return Refus(execution!, escalade.Motif);
        }

        var actionDeForce = definition.Action switch
        {
            ActionsDePilotage.ArreterServiceWindows => ActionsDePilotage.ArreterServiceWindowsDeForce,
            _ => null
        };

        if (actionDeForce is null)
        {
            return Refus(execution!, $"L'action « {definition.Action} » n'a pas de variante forcée.");
        }

        var composant = definition.ComposantCibleId is { } id
            ? await etat.LireLeComposantAsync(id, cancellationToken)
            : null;
        var cible = ResoudreLaCible(composant, definition);

        var resultat = await commandes.ExecuterAsync(
            new DemandeDeCommande(actionDeForce, cible, Timeout: TimeSpan.FromSeconds(definition.TimeoutSecondes)),
            cancellationToken);

        etape.Preuve = $"[Forcé par {acteur.NomAffiche}] {MasquageDesSecrets.Appliquer(resultat.Preuve)}";
        etape.NombreDeTentatives++;

        var statutBrut = EvaluationDeCommande.EvaluerLeResultatBrut(resultat.Resultat);

        if (statutBrut == StepStatus.Echec)
        {
            etape.TypeDErreur = StepErrorKind.ErreurDeCommande;
            etape.MessageDErreur = MasquageDesSecrets.Appliquer(resultat.MotifDEchec ?? resultat.Preuve);
            return await EchouerLExecutionAsync(
                execution!, $"Étape {etape.Ordre} : échec après arrêt forcé — {etape.MessageDErreur}", cancellationToken);
        }

        if (statutBrut == StepStatus.EnCours)
        {
            await etat.EnregistrerAsync(cancellationToken);
            return new ReponseDuMoteur(true, execution!.Statut, resultat.Preuve);
        }

        etape.Statut = StepStatus.Verification;
        await etat.EnregistrerAsync(cancellationToken);

        return await VerifierLEffetEtConclureAsync(execution!, etape, definition, cancellationToken);
    }

    public async Task<ReponseDuMoteur> ConsignerUneInterventionManuelleAsync(
        Guid executionId, Guid etapeId, bool succes, string preuve, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(preuve);

        var (execution, etape, _, erreur) = await ChargerAsync(executionId, etapeId, cancellationToken);
        if (erreur is not null)
        {
            return erreur;
        }

        if (etape!.Statut != StepStatus.Bloque)
        {
            return Refus(execution!,
                $"Étape {etape.Ordre} : une intervention manuelle ne s'applique qu'à une étape bloquée.");
        }

        etape.Statut = succes ? StepStatus.Reussi : StepStatus.Echec;
        // Masquée comme une preuve machine : une preuve saisie à la main contient tout aussi
        // bien un mot de passe recopié depuis une console.
        etape.Preuve = MasquageDesSecrets.Appliquer(preuve);
        etape.Decision = "Intervention manuelle";
        etape.DecidePar = acteur.NomAffiche;
        etape.DecideLe = horloge.MaintenantUtc;
        etape.FinLe ??= horloge.MaintenantUtc;

        if (!succes)
        {
            etape.TypeDErreur = StepErrorKind.ErreurDeCommande;
            etape.MessageDErreur = "Échec constaté par intervention manuelle.";
            return await EchouerLExecutionAsync(
                execution!, $"Étape {etape.Ordre} : échec constaté par intervention manuelle de {acteur.NomAffiche}.",
                cancellationToken);
        }

        await etat.EnregistrerAsync(cancellationToken);
        return new ReponseDuMoteur(true, execution!.Statut,
            $"Étape {etape.Ordre} conclue par intervention manuelle de {acteur.NomAffiche}.");
    }

    // — Sprint 7 : mécanique interne —

    private async Task<ReponseDuMoteur> LancerLEtapeAsync(
        OperationExecution execution,
        ExecutionStep etape,
        WorkflowStepDefinition definition,
        CancellationToken cancellationToken)
    {
        var composant = definition.ComposantCibleId is { } id
            ? await etat.LireLeComposantAsync(id, cancellationToken)
            : null;

        if (definition.ComposantCibleId is not null
            && (composant is null
                || composant.Statut != ValidationStatus.Actif
                || composant.ModeDePilotage != ModeDePilotage.Pilotable))
        {
            etape.Statut = StepStatus.Bloque;
            etape.TypeDErreur = StepErrorKind.PrerequisNonSatisfait;
            etape.MessageDErreur = composant is null
                ? "Composant cible introuvable."
                : $"{composant.Nom} n'est pas actif et pilotable : aucune commande ne peut être émise.";
            await etat.EnregistrerAsync(cancellationToken);
            return new ReponseDuMoteur(true, execution.Statut, etape.MessageDErreur);
        }

        // « Composants déjà arrêtés ignorés proprement, ordre recalculé sans rompre les
        // dépendances » (plan, S7). L'état réel est relu juste avant d'émettre, jamais déduit de
        // l'étape précédente : arrêter un service déjà arrêté renvoie une erreur de commande qui
        // ferait échouer toute la séquence sur un composant pourtant dans l'état voulu.
        //
        // L'ordre n'est pas réordonné pour autant : les étapes restantes gardent leur rang et
        // s'enchaînent inchangées — sauter celle qui n'a plus lieu d'être ne déplace rien.
        var etatVise = ActionsDePilotage.EtatViseParLAction(definition.Action);
        var etatAvantCommande = await LireLEtatConstateAsync(
            execution.EnvironmentId, definition.ComposantCibleId, cancellationToken);

        if (EvaluationDeCommande.EstDejaDansLEtatVise(etatAvantCommande, etatVise))
        {
            etape.Statut = StepStatus.Ignore;
            etape.DebutLe ??= horloge.MaintenantUtc;
            etape.FinLe ??= horloge.MaintenantUtc;
            etape.OperateurExecutant = acteur.NomAffiche;
            etape.Preuve = $"Aucune commande émise : {composant?.Nom ?? "la cible"} est déjà "
                + $"dans l'état visé ({etatVise}), constaté avant émission.";
            await etat.EnregistrerAsync(cancellationToken);

            return new ReponseDuMoteur(true, execution.Statut,
                $"Étape {etape.Ordre} ignorée — {etape.Preuve}");
        }

        // Sprint 8 — les verrous du démarrage complet, appliqués juste avant d'émettre. Ils ne
        // valent que là : évalués à la préparation, ils porteraient sur un état déjà périmé
        // quand la commande partira.
        if (etatVise == ComponentHealth.Operationnel && composant is not null)
        {
            var verrou = await VerifierLesPrerequisDeDemarrageAsync(
                execution, etape, composant, cancellationToken);

            if (!verrou.Autorise)
            {
                etape.Statut = StepStatus.Bloque;
                etape.TypeDErreur = StepErrorKind.PrerequisNonSatisfait;
                etape.MessageDErreur = verrou.Motif;
                await etat.EnregistrerAsync(cancellationToken);
                return new ReponseDuMoteur(true, execution.Statut, verrou.Motif);
            }
        }

        var cible = ResoudreLaCible(composant, definition);

        etape.Statut = StepStatus.EnCours;
        etape.DebutLe ??= horloge.MaintenantUtc;
        etape.OperateurExecutant = acteur.NomAffiche;
        etape.NombreDeTentatives++;
        await etat.EnregistrerAsync(cancellationToken);

        var resultat = await commandes.ExecuterAsync(
            new DemandeDeCommande(definition.Action, cible, Timeout: TimeSpan.FromSeconds(definition.TimeoutSecondes)),
            cancellationToken);

        // Masquée avant persistance, pas seulement avant affichage (SEC-003) : un secret déjà
        // écrit en base est un secret divulgué, que l'écran le montre ou non.
        etape.Preuve = MasquageDesSecrets.Appliquer(resultat.Preuve);

        var statutBrut = EvaluationDeCommande.EvaluerLeResultatBrut(resultat.Resultat);

        if (statutBrut == StepStatus.EnCours)
        {
            // Toujours en cours : le prochain AvancerAsync retentera la vérification, ou un
            // acteur habilité proposera un forçage — jamais automatiquement.
            await etat.EnregistrerAsync(cancellationToken);
            return new ReponseDuMoteur(true, execution.Statut, resultat.Preuve);
        }

        if (statutBrut == StepStatus.Echec)
        {
            etape.TypeDErreur = StepErrorKind.ErreurDeCommande;
            etape.MessageDErreur = MasquageDesSecrets.Appliquer(resultat.MotifDEchec ?? resultat.Preuve);
            return await EchouerLExecutionAsync(
                execution, $"Étape {etape.Ordre} : {etape.MessageDErreur}", cancellationToken);
        }

        // Réussie -> Vérification : l'effet reste à constater, jamais conclu directement.
        etape.Statut = StepStatus.Verification;
        await etat.EnregistrerAsync(cancellationToken);

        return await VerifierLEffetEtConclureAsync(execution, etape, definition, cancellationToken);
    }

    private async Task<ReponseDuMoteur> VerifierLEffetEtConclureAsync(
        OperationExecution execution,
        ExecutionStep etape,
        WorkflowStepDefinition definition,
        CancellationToken cancellationToken)
    {
        var etatConstate = await LireLEtatConstateAsync(
            execution.EnvironmentId, definition.ComposantCibleId, cancellationToken);

        var etatVise = ActionsDePilotage.EtatViseParLAction(definition.Action);
        var resultatDeLaVerification = EvaluationDeCommande.VerifierLEffet(etatConstate, etatVise);

        etape.Statut = resultatDeLaVerification;

        if (resultatDeLaVerification == StepStatus.Echec)
        {
            etape.TypeDErreur = StepErrorKind.EtatInattendu;
            etape.MessageDErreur = "L'état constaté après la commande ne correspond pas à l'état visé.";
            return await EchouerLExecutionAsync(
                execution, $"Étape {etape.Ordre} : {etape.MessageDErreur}", cancellationToken);
        }

        if (resultatDeLaVerification == StepStatus.Bloque)
        {
            etape.MessageDErreur = "État réel non établi après la commande : impossible de conclure automatiquement.";
            await etat.EnregistrerAsync(cancellationToken);
            return new ReponseDuMoteur(true, execution.Statut, etape.MessageDErreur);
        }

        etape.FinLe ??= horloge.MaintenantUtc;
        await etat.EnregistrerAsync(cancellationToken);

        return new ReponseDuMoteur(true, execution.Statut,
            resultatDeLaVerification == StepStatus.Reussi
                ? $"Étape {etape.Ordre} réussie."
                : $"Étape {etape.Ordre} réussie avec avertissement.");
    }

    private async Task<ReponseDuMoteur> ConclureSiPossibleAsync(
        OperationExecution execution, CancellationToken cancellationToken)
    {
        var conclusion = MachineAEtats.ConclureDepuisLesEtapes([.. execution.Etapes.Select(e => e.Statut)]);

        if (!MachineAEtats.EstAutorisee(execution.Statut, conclusion))
        {
            return new ReponseDuMoteur(true, execution.Statut,
                "Aucune étape éligible ; l'exécution attend une décision humaine.");
        }

        execution.Statut = conclusion;
        execution.FinLe = horloge.MaintenantUtc;
        await etat.EnregistrerAsync(cancellationToken);
        await etat.LibererLeVerrouAsync(execution.Id, acteur.NomAffiche, cancellationToken);

        return new ReponseDuMoteur(true, execution.Statut, "Exécution conclue : plus aucune étape à traiter.");
    }

    private async Task<ReponseDuMoteur> EchouerLExecutionAsync(
        OperationExecution execution, string motif, CancellationToken cancellationToken)
    {
        execution.Statut = ExecutionStatus.Echec;
        execution.Resultat = motif;
        execution.FinLe = horloge.MaintenantUtc;
        await etat.EnregistrerAsync(cancellationToken);
        await etat.LibererLeVerrouAsync(execution.Id, acteur.NomAffiche, cancellationToken);

        return new ReponseDuMoteur(false, execution.Statut, motif);
    }

    private async Task<(OperationExecution? Execution, ExecutionStep? Etape, WorkflowStepDefinition? Definition, ReponseDuMoteur? Erreur)>
        ChargerAsync(Guid executionId, Guid etapeId, CancellationToken cancellationToken)
    {
        var execution = await etat.LireAsync(executionId, cancellationToken);
        if (execution is null)
        {
            return (null, null, null, new ReponseDuMoteur(false, ExecutionStatus.EnPreparation, "Exécution introuvable."));
        }

        var etape = execution.Etapes.FirstOrDefault(e => e.Id == etapeId);
        if (etape is null)
        {
            return (execution, null, null, Refus(execution, "Étape introuvable sur cette exécution."));
        }

        var definitions = await etat.LireLesDefinitionsDEtapesAsync(execution.WorkflowVersionId, cancellationToken);
        var definition = definitions.FirstOrDefault(d => d.Id == etape.WorkflowStepDefinitionId);
        if (definition is null)
        {
            return (execution, etape, null, Refus(execution, "Définition de l'étape introuvable."));
        }

        return (execution, etape, definition, null);
    }

    /// <summary>
    /// Sprint 8 — aucun composant ne doit être resté debout avant d'engager un démarrage
    /// complet. Démarrer par-dessus produit les incidents les plus difficiles à diagnostiquer :
    /// deux instances du même rôle, des files consommées deux fois, un cluster qui refuse un
    /// nœud déjà membre.
    ///
    /// Seuls les composants pilotables sont considérés. Une base de données en supervision
    /// seule est censée rester debout : l'exiger arrêtée refuserait tout démarrage.
    /// </summary>
    private async Task<VerdictDeDemarrage> VerifierQueRienNEstDeboutAsync(
        OperationExecution execution,
        CancellationToken cancellationToken)
    {
        var cartographie = await supervision.LireAsync(execution.EnvironmentId, cancellationToken);
        if (cartographie is null)
        {
            return new VerdictDeDemarrage(
                false,
                "Aucune cartographie disponible : impossible de confirmer que l'écosystème est "
                + "à l'arrêt, donc impossible d'engager un démarrage complet.",
                []);
        }

        var etats = cartographie.Lignes
            .Where(l => l.ModeDePilotage == ModeDePilotage.Pilotable)
            .Select(l => new EtatConstateDUnComposant(l.Nom, l.Kind, Convertir(l.Etat.Etat)))
            .ToList();

        return ControlesDeDemarrage.VerifierQueToutEstArrete(etats);
    }

    /// <summary>
    /// Sprint 8 — prérequis propres au démarrage d'un composant, évalués sur l'état relu.
    ///
    /// Deux verrous sont branchés ici, ceux dont la supervision porte déjà les signaux : XPS
    /// attend un Bridge confirmé opérationnel, et un nœud de cluster attend que le précédent
    /// ait terminé son initialisation.
    ///
    /// Le troisième verrou du sprint — deux Center détenant simultanément le rôle actif — reste
    /// hors d'atteinte : aucun signal collecté ne distingue aujourd'hui l'instance active de
    /// celle en veille, et le déduire de l'état du service serait précisément l'erreur que la
    /// règle existe pour empêcher. La règle est écrite et testée en domaine ; elle attend sa
    /// source, pas son implémentation.
    /// </summary>
    private async Task<VerdictDeDemarrage> VerifierLesPrerequisDeDemarrageAsync(
        OperationExecution execution,
        ExecutionStep etape,
        N4Component composant,
        CancellationToken cancellationToken)
    {
        var cartographie = await supervision.LireAsync(execution.EnvironmentId, cancellationToken);
        if (cartographie is null)
        {
            return new VerdictDeDemarrage(
                false,
                "Aucune cartographie disponible : les prérequis de démarrage ne peuvent pas être "
                + "vérifiés, donc pas être tenus pour satisfaits.",
                []);
        }

        var etats = cartographie.Lignes.ToDictionary(
            l => l.ComposantId,
            l => new EtatConstateDUnComposant(l.Nom, l.Kind, Convertir(l.Etat.Etat)));

        if (composant.Kind == N4ComponentKind.Xps)
        {
            var bridge = etats.Values.FirstOrDefault(e => e.Kind == N4ComponentKind.BridgeDaemon);
            return ControlesDeDemarrage.VerifierLePrerequisDeXps(bridge);
        }

        if (composant.Kind == N4ComponentKind.ClusterNode)
        {
            return ControlesDeDemarrage.VerifierLeNoeudPrecedent(
                TrouverLeNoeudPrecedent(execution, etape, etats));
        }

        return new VerdictDeDemarrage(true, "Aucun prérequis particulier pour ce rôle.", []);
    }

    /// <summary>
    /// Nœud de cluster démarré juste avant celui-ci dans la séquence. Cherché dans les étapes de
    /// l'exécution et non dans le référentiel : c'est l'ordre du workflow qui fait foi, pas
    /// l'ordre alphabétique des composants.
    /// </summary>
    private static EtatConstateDUnComposant? TrouverLeNoeudPrecedent(
        OperationExecution execution,
        ExecutionStep etape,
        Dictionary<Guid, EtatConstateDUnComposant> etats) =>
        execution.Etapes
            .Where(e => e.Ordre < etape.Ordre && e.ComposantCibleId is not null)
            .OrderByDescending(e => e.Ordre)
            .Select(e => etats.TryGetValue(e.ComposantCibleId!.Value, out var trouve) ? trouve : null)
            .FirstOrDefault(e => e?.Kind == N4ComponentKind.ClusterNode);

    /// <summary>
    /// État réel d'un composant, relu à la supervision. Utilisé aux deux bouts d'une commande :
    /// avant, pour ne pas ré-agir sur une cible déjà dans l'état visé ; après, pour constater
    /// l'effet. <c>null</c> quand l'étape ne vise aucun composant, ou quand rien n'est connu —
    /// les deux cas se traitent pareil : on ne conclut pas.
    /// </summary>
    private async Task<ComponentHealth?> LireLEtatConstateAsync(
        Guid environnementId,
        Guid? composantId,
        CancellationToken cancellationToken)
    {
        if (composantId is not { } id)
        {
            return null;
        }

        var cartographie = await supervision.LireAsync(environnementId, cancellationToken);
        return Convertir(cartographie?.Lignes.FirstOrDefault(l => l.ComposantId == id)?.Etat.Etat);
    }

    /// <summary>
    /// Ce que la commande vise : nom de service (avec préfixe machine éventuel) ou nom/PID de
    /// processus, selon le mécanisme déclaré du composant — jamais une ligne de commande.
    /// </summary>
    private static string ResoudreLaCible(N4Component? composant, WorkflowStepDefinition definition) =>
        composant?.NomDuService ?? composant?.Mecanisme ?? composant?.Nom ?? definition.Action;

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

    private static ComponentHealth? Convertir(Domain.Supervision.EtatDeSupervision? etat) => etat switch
    {
        null => null,
        Domain.Supervision.EtatDeSupervision.Disponible => ComponentHealth.Operationnel,
        Domain.Supervision.EtatDeSupervision.Degrade => ComponentHealth.Degrade,
        Domain.Supervision.EtatDeSupervision.Indisponible => ComponentHealth.Arrete,
        Domain.Supervision.EtatDeSupervision.AConfirmer => ComponentHealth.AConfirmer,
        _ => ComponentHealth.Inconnu
    };

    private static ComponentHealth Convertir(Domain.Supervision.EtatDeSupervision etat) =>
        Convertir((Domain.Supervision.EtatDeSupervision?)etat)!.Value;

    private static ReponseDuMoteur Refus(OperationExecution execution, string motif) =>
        new(false, execution.Statut, motif);
}
