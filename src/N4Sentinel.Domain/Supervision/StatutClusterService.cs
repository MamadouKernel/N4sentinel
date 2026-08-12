namespace N4Sentinel.Domain.Supervision;

/// <summary>
/// Statuts de la vue Cluster Services de N4.
///
/// **Liste non refermée, et c'est délibéré.** Ces valeurs sont celles effectivement relevées
/// dans le guide « Navis N4 3.8.25 — Setup, Maintenance and System Diagnostics ». Le plan de
/// sprints en annonce huit ; le guide n'en atteste que sept. Plutôt que d'inventer la huitième,
/// toute valeur non reconnue est ramenée à <see cref="Inconnu"/> — ce qui, dans la
/// consolidation, produit « À confirmer » et non « opérationnel ».
///
/// À confirmer contre une vue Cluster Services réelle lors de l'atelier Infrastructure.
/// </summary>
public enum StatutClusterService
{
    /// <summary>Statut absent, illisible ou non reconnu. Jamais assimilé à une absence d'anomalie.</summary>
    Inconnu,

    /// <summary>ACTIVE — le service participe au cluster.</summary>
    Actif,

    /// <summary>INACTIVE — le service est connu mais ne participe pas.</summary>
    Inactif,

    /// <summary>INITIALIZING — démarrage en cours, pas encore exploitable.</summary>
    EnInitialisation,

    /// <summary>STARTING — service en cours de lancement.</summary>
    EnDemarrage,

    /// <summary>DISCONNECTED — plus de heartbeat ; N4 attend jusqu'à 30 secondes avant de le refléter.</summary>
    Deconnecte,

    /// <summary>FAILED — le service a échoué.</summary>
    EnEchec,

    /// <summary>UNKNOWN — N4 lui-même déclare ne pas savoir.</summary>
    DeclareInconnuParN4
}

/// <summary>Lecture des statuts Cluster Services tels que N4 les écrit.</summary>
public static class LectureDuStatutClusterService
{
    /// <summary>
    /// Traduit la valeur brute lue dans la vue Cluster Services. Toute valeur inattendue devient
    /// <see cref="StatutClusterService.Inconnu"/> : mieux vaut un état à confirmer qu'un statut
    /// deviné à partir d'une chaîne que l'on ne connaît pas.
    /// </summary>
    public static StatutClusterService Lire(string? valeur) =>
        (valeur ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "ACTIVE" => StatutClusterService.Actif,
            "INACTIVE" => StatutClusterService.Inactif,
            "INITIALIZING" => StatutClusterService.EnInitialisation,
            "STARTING" => StatutClusterService.EnDemarrage,
            "DISCONNECTED" => StatutClusterService.Deconnecte,
            "FAILED" => StatutClusterService.EnEchec,
            "UNKNOWN" => StatutClusterService.DeclareInconnuParN4,
            _ => StatutClusterService.Inconnu
        };

    /// <summary>
    /// Verdict qu'un statut Cluster Services apporte à la consolidation.
    /// Seul ACTIVE est favorable : un nœud en initialisation n'est pas encore opérationnel,
    /// et le cahier des charges impose qu'il soit pleinement ACTIVE avant de lancer le suivant.
    /// </summary>
    public static VerdictDeSignal Verdict(StatutClusterService statut) => statut switch
    {
        StatutClusterService.Actif => VerdictDeSignal.Favorable,
        StatutClusterService.Inactif => VerdictDeSignal.Defavorable,
        StatutClusterService.EnEchec => VerdictDeSignal.Defavorable,
        StatutClusterService.Deconnecte => VerdictDeSignal.Defavorable,
        StatutClusterService.EnInitialisation => VerdictDeSignal.Degrade,
        StatutClusterService.EnDemarrage => VerdictDeSignal.Degrade,
        _ => VerdictDeSignal.Indisponible
    };
}
