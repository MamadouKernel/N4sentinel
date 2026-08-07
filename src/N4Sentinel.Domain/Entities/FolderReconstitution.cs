using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Procédure de reconstitution sécurisée d'un dossier partagé après suspicion ou confirmation de corruption
/// (E5.2). FR-059E impose une séquence fixe une fois validée par un utilisateur habilité : arrêt des
/// composants requis, sauvegarde préalable, vérification de l'intégrité de la sauvegarde, reconstitution,
/// redémarrage contrôlé et tests finaux. Chaque étape exige une confirmation explicite et peut porter une
/// preuve (FR-059F, mode SOP pas-à-pas) ; rien n'avance sans ce geste humain.
///
/// Décision de cadrage (Sprint 11) : contrairement à la lecture littérale de FR-059E ("exécuter... un
/// workflow approuvé"), cette procédure ne déclenche ici aucune action réelle de fichier ou de service — le
/// cahier des charges lui-même situe l'automatisation de bout en bout au Palier 2 (hors périmètre V1, cf.
/// section "Niveau d'automatisation" : le Palier 1 retenu pour V1 reste "semi-automatique" avec confirmation
/// humaine à chaque action mutative). Tant qu'aucun connecteur réel n'existe (cf. décision Sprint 2/10 :
/// Simulation uniquement), FolderReconstitution est un guide SOP tracé et audité (FR-059F/G), pas une
/// automatisation. Elle ne réutilise ni le moteur <see cref="Workflow"/>/<see cref="OperationRun"/> (qui cible
/// des <see cref="N4Component"/>, pas un <see cref="SharedFolder"/>) ni la future entité SOP versionnée
/// d'E9.3 (FR-089, Sprint 15) — ce serait un couplage prématuré à un concept qui n'existe pas encore.
/// </summary>
public class FolderReconstitution
{
    private readonly List<ReconstitutionStepRecord> _steps = [];

    private static readonly ReconstitutionStepKind[] OrderedSteps =
    [
        ReconstitutionStepKind.StopComponents,
        ReconstitutionStepKind.Backup,
        ReconstitutionStepKind.VerifyBackupIntegrity,
        ReconstitutionStepKind.Reconstruct,
        ReconstitutionStepKind.ControlledRestart,
        ReconstitutionStepKind.FinalTests,
    ];

    private FolderReconstitution()
    {
        Reason = string.Empty;
        StartedByUserId = string.Empty;
    }

    public FolderReconstitution(Guid sharedFolderId, string reason, string startedByUserId)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainRuleException(
                "Le motif de la reconstitution est obligatoire (FR-059G, traçabilité).");
        }

        if (string.IsNullOrWhiteSpace(startedByUserId))
        {
            throw new DomainRuleException("L'utilisateur habilité à l'origine de la reconstitution est obligatoire.");
        }

        Id = Guid.NewGuid();
        SharedFolderId = sharedFolderId;
        Reason = reason.Trim();
        StartedByUserId = startedByUserId.Trim();
        StartedAtUtc = DateTime.UtcNow;
        Status = ReconstitutionStatus.InProgress;
    }

    public Guid Id { get; private set; }

    public Guid SharedFolderId { get; private set; }

    public string Reason { get; private set; }

    public string StartedByUserId { get; private set; }

    public DateTime StartedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public ReconstitutionStatus Status { get; private set; }

    public string? AbortReason { get; private set; }

    public IReadOnlyList<ReconstitutionStepRecord> Steps => _steps.OrderBy(s => s.Position).ToList();

    /// <summary>Prochaine étape de la séquence fixe FR-059E à confirmer, ou null si terminée/abandonnée.</summary>
    public ReconstitutionStepKind? NextStep =>
        Status == ReconstitutionStatus.InProgress && _steps.Count < OrderedSteps.Length
            ? OrderedSteps[_steps.Count]
            : null;

    /// <summary>
    /// Confirme la prochaine étape de la séquence FR-059E, avec preuve optionnelle (FR-059F). Termine
    /// automatiquement la reconstitution une fois la dernière étape (tests finaux) confirmée.
    /// </summary>
    public void ConfirmNextStep(string confirmedByUserId, string? evidence)
    {
        if (Status != ReconstitutionStatus.InProgress)
        {
            throw new DomainRuleException("Cette reconstitution n'est plus en cours.");
        }

        if (string.IsNullOrWhiteSpace(confirmedByUserId))
        {
            throw new DomainRuleException("L'utilisateur qui confirme l'étape est obligatoire.");
        }

        var next = NextStep ?? throw new DomainRuleException("Toutes les étapes ont déjà été confirmées.");

        _steps.Add(new ReconstitutionStepRecord(Id, next, _steps.Count, confirmedByUserId, evidence));

        if (_steps.Count == OrderedSteps.Length)
        {
            Status = ReconstitutionStatus.Completed;
            CompletedAtUtc = DateTime.UtcNow;
        }
    }

    /// <summary>Abandon tracé (cas incertain à escalader — FR-059G) : n'annule aucune étape déjà confirmée.</summary>
    public void Abort(string reason)
    {
        if (Status != ReconstitutionStatus.InProgress)
        {
            throw new DomainRuleException("Cette reconstitution n'est plus en cours.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainRuleException("Le motif d'abandon est obligatoire (FR-059G, traçabilité).");
        }

        Status = ReconstitutionStatus.Aborted;
        AbortReason = reason.Trim();
        CompletedAtUtc = DateTime.UtcNow;
    }
}
