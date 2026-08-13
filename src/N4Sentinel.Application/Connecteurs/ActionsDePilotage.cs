using N4Sentinel.Domain.Common;

namespace N4Sentinel.Application.Connecteurs;

/// <summary>
/// SEC-006 — catalogue fermé des actions de pilotage. Ferme l'écart noté dans
/// <c>docs/architecture.md</c> (« Catalogue d'actions : S5 », jamais livré) :
/// <c>WorkflowStepDefinition.Action</c> était un texte libre depuis le Sprint 0, jamais validé.
/// Une étape ne peut désormais désigner qu'une action de cette liste — jamais une commande
/// saisie librement.
/// </summary>
public static class ActionsDePilotage
{
    public const string ArreterServiceWindows = "ArreterServiceWindows";
    public const string DemarrerServiceWindows = "DemarrerServiceWindows";
    public const string RedemarrerServiceWindows = "RedemarrerServiceWindows";

    /// <summary>Réservée à l'escalade « bloqué » : jamais lancée d'elle-même (plan de sprints, S7).</summary>
    public const string ArreterServiceWindowsDeForce = "ArreterServiceWindowsDeForce";

    /// <summary>Modélise l'arrêt « par gestionnaire de tâches » du Standby Center Node.</summary>
    public const string ArreterProcessus = "ArreterProcessus";

    public static IReadOnlyList<string> Toutes { get; } =
    [
        ArreterServiceWindows,
        DemarrerServiceWindows,
        RedemarrerServiceWindows,
        ArreterServiceWindowsDeForce,
        ArreterProcessus
    ];

    /// <summary>
    /// État visé une fois l'action réellement passée — utilisé pour vérifier l'effet, jamais
    /// pour le supposer. <c>null</c> pour une action sans état visé modélisé.
    /// </summary>
    public static ComponentHealth? EtatViseParLAction(string action) => action switch
    {
        ArreterServiceWindows or ArreterServiceWindowsDeForce or ArreterProcessus => ComponentHealth.Arrete,
        DemarrerServiceWindows or RedemarrerServiceWindows => ComponentHealth.Operationnel,
        _ => null
    };
}
