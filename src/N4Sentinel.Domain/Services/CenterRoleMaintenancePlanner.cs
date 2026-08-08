using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Domain.Services;

/// <summary>Choix de continuité demandé à l'opérateur avant toute action sur le Center primaire (FR-046).</summary>
public enum CenterRoleStrategy
{
    /// <summary>
    /// Le rôle actif doit rester sur le primaire. Impose d'arrêter le Standby en premier, faute de quoi il
    /// prendrait le verrou dès l'arrêt du primaire et provoquerait une bascule non voulue.
    /// </summary>
    KeepRoleOnPrimary = 0,

    /// <summary>Bascule assumée : le Standby prend le rôle (FR-047).</summary>
    AcceptFailover = 1,
}

public sealed record CenterMaintenanceStep(
    int Position,
    string Label,
    Guid? ComponentId,
    string? ComponentName,
    WorkflowStepAction Action,
    string SuccessCriteria,
    bool IsVerification,
    string? SourceReference);

public sealed record CenterMaintenancePlan(
    CenterRoleStrategy Strategy,
    string Summary,
    IReadOnlyList<CenterMaintenanceStep> Steps,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Construit le déroulé d'une intervention sur le couple Center / Standby Center (FR-046, FR-047).
///
/// Le point dur n'est pas l'ordre des services mais le **verrou** : un seul des deux Center peut détenir le
/// rôle actif à la fois, arbitré par un verrou base de données (défaut depuis N4 3.3) ou fichier via ActiveMQ
/// <c>[GUIDE p.450-451]</c>. Conséquence directe : arrêter le primaire alors que le Standby tourne provoque
/// une bascule — ce n'est pas une panne, c'est le comportement nominal. D'où l'obligation de FR-046 de
/// demander l'intention avant d'agir.
///
/// Pour conserver le rôle sur le primaire, Navis prescrit : arrêter le service Center « on both the Center
/// node and the standby Center node », puis « restart the N4 Navis Center Node service on the main Center
/// node and wait until it becomes active », et enfin « once the Center node is active, then start N4 on the
/// Standby node » <c>[GUIDE §1.10.4 p.451]</c>.
///
/// Chaque plan se termine par une vérification d'unicité du rôle actif : FR-047 interdit toute situation où
/// deux Center seraient actifs.
/// </summary>
public static class CenterRoleMaintenancePlanner
{
    private const string SingleActiveCriteria =
        "Exactement une instance Center détient le rôle actif dans Cluster Services / Node Info Desk.";

    public static CenterMaintenancePlan Plan(
        IReadOnlyCollection<N4Component> components, CenterRoleStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(components);

        var primaries = components.Where(c => c.Kind == N4ComponentKind.CenterNode).ToList();
        var standbys = components.Where(c => c.Kind == N4ComponentKind.StandbyCenterNode).ToList();

        if (primaries.Count == 0)
        {
            throw new DomainRuleException(
                "Aucun Center Node n'est déclaré dans cet environnement : l'intervention est sans objet.");
        }

        if (primaries.Count > 1)
        {
            throw new DomainRuleException(
                "Plusieurs composants sont déclarés Center Node. Un environnement N4 n'a qu'un Center primaire ; " +
                "le second est un Standby Center Node. Corrigez le typage du référentiel avant d'intervenir.");
        }

        if (standbys.Count > 1)
        {
            throw new DomainRuleException(
                "Plusieurs composants sont déclarés Standby Center Node. Un seul nœud de bascule est prévu.");
        }

        var primary = primaries[0];
        var standby = standbys.FirstOrDefault();

        EnsureControllable(primary);

        if (standby is not null)
        {
            EnsureControllable(standby);
        }

        return strategy switch
        {
            CenterRoleStrategy.KeepRoleOnPrimary => PlanKeepRole(primary, standby),
            CenterRoleStrategy.AcceptFailover => PlanFailover(primary, standby),
            _ => throw new DomainRuleException($"Stratégie de continuité inconnue : « {strategy} »."),
        };
    }

    private static void EnsureControllable(N4Component component)
    {
        if (component.Governance != ComponentGovernance.Controllable)
        {
            throw new DomainRuleException(
                $"« {component.Name} » n'est pas déclaré pilotable : aucune action ne peut être déclenchée dessus.");
        }
    }

    private static CenterMaintenancePlan PlanKeepRole(N4Component primary, N4Component? standby)
    {
        var steps = new List<CenterMaintenanceStep>();
        var warnings = new List<string>();
        var position = 0;

        steps.Add(new CenterMaintenanceStep(
            ++position, "Vérifier quelle instance détient le rôle actif", null, null,
            WorkflowStepAction.HealthCheck,
            "Le primaire détient bien le rôle actif. S'il est déjà porté par le Standby, cette stratégie ne " +
            "s'applique pas : le rôle devrait d'abord être ramené sur le primaire.",
            IsVerification: true, "FR-032, FR-046"));

        if (standby is null)
        {
            warnings.Add(
                "Aucun Standby Center Node n'est déclaré : il n'y a pas de risque de bascule, mais aucune " +
                "reprise non plus pendant l'indisponibilité du primaire.");
        }
        else
        {
            // L'étape déterminante : sans elle, l'arrêt du primaire déclencherait une bascule non voulue,
            // le Standby prenant le verrou.
            steps.Add(new CenterMaintenanceStep(
                ++position, $"Arrêter le Standby — {standby.Name}", standby.Id, standby.Name,
                WorkflowStepAction.Stop,
                "Service arrêté. Cet arrêt précède celui du primaire : sinon le Standby prendrait le verrou " +
                "et deviendrait actif.",
                IsVerification: false, "GUIDE §1.10.4 p.451 · FR-046"));
        }

        steps.Add(new CenterMaintenanceStep(
            ++position, $"Arrêter le Center primaire — {primary.Name}", primary.Id, primary.Name,
            WorkflowStepAction.Stop,
            "Service arrêté ; état final des files et journaux conservé.",
            IsVerification: false, "GUIDE §1.10.4 p.451"));

        steps.Add(new CenterMaintenanceStep(
            ++position, $"Démarrer le Center primaire — {primary.Name}", primary.Id, primary.Name,
            WorkflowStepAction.Start,
            "Statut ACTIVE dans Cluster Services. Le démarrage peut prendre plusieurs minutes ; ne pas " +
            "enchaîner avant confirmation.",
            IsVerification: false, "GUIDE §1.10.4 p.451"));

        steps.Add(new CenterMaintenanceStep(
            ++position, "Confirmer que le primaire a repris le rôle actif", null, null,
            WorkflowStepAction.HealthCheck,
            "Le primaire est ACTIVE et détient le rôle. Condition impérative avant de relancer le Standby.",
            IsVerification: true, "GUIDE §1.10.4 p.451 · FR-032"));

        if (standby is not null)
        {
            steps.Add(new CenterMaintenanceStep(
                ++position, $"Redémarrer le Standby — {standby.Name}", standby.Id, standby.Name,
                WorkflowStepAction.Start,
                "Service démarré. Le statut « Initializing » dans Node Info Desk est le comportement attendu " +
                "tant qu'il n'a pas pris le relais.",
                IsVerification: false, "GUIDE §1.10.4 p.451 · FR-033"));
        }

        steps.Add(new CenterMaintenanceStep(
            ++position, "Contrôle final d'unicité du rôle actif", null, null,
            WorkflowStepAction.HealthCheck, SingleActiveCriteria, IsVerification: true, "FR-047"));

        return new CenterMaintenancePlan(
            CenterRoleStrategy.KeepRoleOnPrimary,
            "Le rôle actif reste sur le primaire. Le Standby est arrêté en premier pour empêcher une bascule, " +
            "puis remis en service une fois le primaire redevenu actif.",
            steps,
            warnings);
    }

    private static CenterMaintenancePlan PlanFailover(N4Component primary, N4Component? standby)
    {
        if (standby is null)
        {
            throw new DomainRuleException(
                "Aucun Standby Center Node n'est déclaré : il n'y a aucune instance vers laquelle basculer. " +
                "Utilisez la stratégie de continuité sur le primaire.");
        }

        var steps = new List<CenterMaintenanceStep>();
        var position = 0;

        steps.Add(new CenterMaintenanceStep(
            ++position, $"Vérifier que le Standby est apte à prendre le rôle — {standby.Name}",
            standby.Id, standby.Name, WorkflowStepAction.HealthCheck,
            "Service démarré, en attente (« Initializing »), accès au dossier partagé et à la base confirmés. " +
            "Une bascule vers un Standby inapte laisserait l'environnement sans Center actif.",
            IsVerification: true, "FR-047, FR-033"));

        steps.Add(new CenterMaintenanceStep(
            ++position, $"Arrêter le Center primaire — {primary.Name}", primary.Id, primary.Name,
            WorkflowStepAction.Stop,
            "Service arrêté. Le verrou est libéré : le Standby prend le rôle actif. C'est le comportement " +
            "nominal du mécanisme de verrou, pas un incident.",
            IsVerification: false, "GUIDE p.450-451 · FR-047"));

        steps.Add(new CenterMaintenanceStep(
            ++position, $"Confirmer la prise de rôle par le Standby — {standby.Name}",
            standby.Id, standby.Name, WorkflowStepAction.HealthCheck,
            "Le Standby apparaît désormais actif dans Cluster Services et les nœuds applicatifs y sont " +
            "raccrochés.",
            IsVerification: true, "FR-032, FR-047"));

        steps.Add(new CenterMaintenanceStep(
            ++position, "Contrôle final d'unicité du rôle actif", null, null,
            WorkflowStepAction.HealthCheck,
            SingleActiveCriteria + " Le primaire ne doit pas être relancé tant que ce contrôle n'est pas fait.",
            IsVerification: true, "FR-047"));

        return new CenterMaintenancePlan(
            CenterRoleStrategy.AcceptFailover,
            $"Bascule assumée vers « {standby.Name} ». Le primaire est arrêté après vérification de l'aptitude " +
            "du Standby, qui prend alors le rôle actif.",
            steps,
            // Le plan s'arrête volontairement avant le redémarrage du primaire : le relancer sans contrôle
            // préalable est exactement le scénario « deux Center actifs » que FR-047 interdit.
            [
                "Le redémarrage du primaire n'est pas inclus : il ne doit intervenir qu'après confirmation " +
                "que le Standby détient seul le rôle, sous peine de se retrouver avec deux Center actifs.",
            ]);
    }
}
