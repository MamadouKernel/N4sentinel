using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Un dossier partagé N4 rattaché à un environnement (configuration, stockage ActiveMQ/KahaDB, échanges EDI,
/// archives, dossier d'erreur — FR-059A). Jamais la cible d'une étape de workflow (aucune action de pilotage
/// N4 Sentinel n'a de sens dessus) — distinct de <see cref="N4Component"/> pour la même raison que
/// <see cref="DependentSystem"/> (E1.6, Sprint 7). Porte son propre instantané de santé (FR-059B/FR-059D)
/// plutôt que le vocabulaire <see cref="ComponentHealthStatus"/>, qui décrit un cycle de vie de service, pas
/// des dimensions de capacité/structure.
/// </summary>
public class SharedFolder
{
    private SharedFolder()
    {
        Name = string.Empty;
        Path = string.Empty;
    }

    public SharedFolder(Guid environmentId, string name, SharedFolderCategory category, string path)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("Le nom du dossier partagé est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new DomainRuleException("Le chemin du dossier partagé est obligatoire.");
        }

        Id = Guid.NewGuid();
        EnvironmentId = environmentId;
        Name = name.Trim();
        Category = category;
        Path = path.Trim();
        CorruptionStatus = CorruptionStatus.None;
    }

    public Guid Id { get; private set; }

    public Guid EnvironmentId { get; private set; }

    public string Name { get; private set; }

    public SharedFolderCategory Category { get; private set; }

    public string Path { get; private set; }

    /// <summary>Accessibilité, droits, capacité de lecture/écriture non destructive (FR-059B).</summary>
    public bool IsAccessible { get; private set; } = true;

    /// <summary>Espace utilisé en pourcentage (FR-059B) — null si jamais vérifié.</summary>
    public int? UsedCapacityPercent { get; private set; }

    /// <summary>Structure attendue et fichiers obligatoires présents (FR-059B).</summary>
    public bool StructureValid { get; private set; } = true;

    /// <summary>Suspicion vs corruption confirmée (FR-059D).</summary>
    public CorruptionStatus CorruptionStatus { get; private set; }

    public string? AnomalyDescription { get; private set; }

    public DateTime? LastCheckedUtc { get; private set; }

    private const int CapacityAlertThresholdPercent = 90;

    /// <summary>Seuils simples documentés ici, pas un moteur de règles — cf. décision Sprint 10.</summary>
    public bool HasAnomaly =>
        !IsAccessible ||
        !StructureValid ||
        CorruptionStatus != CorruptionStatus.None ||
        UsedCapacityPercent >= CapacityAlertThresholdPercent;

    public void RecordHealthCheck(
        bool isAccessible, int? usedCapacityPercent, bool structureValid,
        CorruptionStatus corruptionStatus, string? anomalyDescription)
    {
        IsAccessible = isAccessible;
        UsedCapacityPercent = usedCapacityPercent;
        StructureValid = structureValid;
        CorruptionStatus = corruptionStatus;
        AnomalyDescription = anomalyDescription?.Trim();
        LastCheckedUtc = DateTime.UtcNow;
    }
}
