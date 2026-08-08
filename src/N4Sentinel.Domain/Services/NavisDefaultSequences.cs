using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Domain.Services;

/// <summary>
/// Séquences d'arrêt et de démarrage telles que documentées par Navis, fournies comme **valeurs initiales**
/// de la configuration. Elles ne sont pas une contrainte du code : une fois créées en base, un administrateur
/// peut les réordonner, en retirer des paliers ou en créer de nouvelles versions.
///
/// Sources : arrêt <c>[GUIDE §1.10.7 p.455]</c> et <c>[ITADMIN sl.178-179]</c> ; démarrage
/// <c>[GUIDE §1.10.9 p.457-458]</c> et <c>[ITADMIN sl.112-119]</c>.
///
/// Les deux séquences sont écrites indépendamment, et c'est délibéré : le démarrage n'est pas l'inverse de
/// l'arrêt. Les Cluster Nodes sont premiers au démarrage (« You must start the N4 Cluster nodes before you
/// start the Center node, the Standby Center node, the Bridge daemon, and XPS » <c>[GUIDE p.458]</c>) mais
/// avant-derniers à l'arrêt. Le Center Node les suit immédiatement dans les deux cas — ce qui le place en
/// 2ᵉ position au démarrage et en dernière position à l'arrêt.
/// </summary>
public static class NavisDefaultSequences
{
    public const string StopTemplateKey = "SEQ-N4-ARRET";
    public const string StartTemplateKey = "SEQ-N4-DEMARRAGE";

    private const string ClusterActiveCriteria =
        "Statut ACTIVE dans la vue Cluster Services, nœud complètement initialisé, avant de passer au suivant.";

    /// <summary>Séquence d'arrêt par défaut, à l'état Actif et prête à l'emploi.</summary>
    public static SequenceTemplate CreateStopSequence()
    {
        var template = new SequenceTemplate(
            StopTemplateKey,
            WorkflowType.Stop,
            "Arrêt complet N4 (séquence Navis)",
            "Ordre d'arrêt documenté par Navis (GUIDE §1.10.7 p.455) et repris par le cahier des charges §8.4.");

        // Étapes 1 et 2 du cahier des charges §8.4 : les postes clients ne sont pas des services pilotables,
        // leur arrêt est constaté par l'opérateur avant que la séquence technique ne commence.
        template.AddCheckpoint(
            "Confirmer l'arrêt des activités et des clients ECN4Web / Billing",
            "Confirmation opérateur, ou intégration disponible.", "CDC §8.4 étape 1");

        template.AddCheckpoint(
            "Arrêter les clients XPS, y compris les clients serveur",
            "Processus ou connexion absents, ou confirmation opérateur.", "CDC §8.4 étape 2");

        template.AddTier(
            N4ComponentKind.Ecn4Web, "ECN4Web", SequenceTierExecution.Sequential,
            "Service arrêté, plus aucune session ECN4Web active.", isOptional: true, null, "GUIDE p.455 §1.10.7-3");

        template.AddTier(
            N4ComponentKind.Ecn4Daemon, "ECN4 (daemon)", SequenceTierExecution.Sequential,
            "Service arrêté.", isOptional: true, null, "GUIDE p.455 §1.10.7-4");

        template.AddTier(
            N4ComponentKind.XpsServer, "XPS", SequenceTierExecution.Sequential,
            "Les quatre sous-services XPS sont arrêtés.", isOptional: false, null, "GUIDE p.455 §1.10.7-5");

        template.AddTier(
            N4ComponentKind.XpsBridgeDaemon, "XPS Bridge Daemon", SequenceTierExecution.Sequential,
            "Service arrêté, file de messages vers le Center Node vidée.", isOptional: false, null,
            "GUIDE p.455 §1.10.7-6");

        template.AddTier(
            N4ComponentKind.StandbyCenterNode, "Standby Center Node", SequenceTierExecution.Sequential,
            "Service arrêté. Son statut « INITIALIZING » peut imposer un arrêt via le gestionnaire de tâches.",
            isOptional: true, null, "GUIDE p.455 §1.10.7-7");

        // Séquentiel et non parallèle : arrêter tous les nœuds ensemble fait tourner Hazelcast en rond pour
        // redistribuer les caches et provoque une expiration à 10 minutes [GUIDE p.455, Notes].
        template.AddTier(
            N4ComponentKind.ClusterNode, "Cluster Nodes", SequenceTierExecution.Sequential,
            "Service arrêté et cache Hazelcast redistribué (compter 30 s à 1 min par nœud).",
            isOptional: false, settleDelaySeconds: 60, "GUIDE p.455 §1.10.7-8 + Notes");

        template.AddTier(
            N4ComponentKind.CenterNode, "Center Node", SequenceTierExecution.Sequential,
            "Service arrêté en dernier : il porte la liaison des nœuds applicatifs à la base.",
            isOptional: false, null, "GUIDE p.455 §1.10.7-9");

        template.AddCheckpoint(
            "Contrôle final",
            "Tous les composants ciblés sont arrêtés ; rapport généré.", "CDC §8.4 étape 10");

        Publish(template);
        return template;
    }

    /// <summary>Séquence de démarrage par défaut, à l'état Actif et prête à l'emploi.</summary>
    public static SequenceTemplate CreateStartSequence()
    {
        var template = new SequenceTemplate(
            StartTemplateKey,
            WorkflowType.Start,
            "Démarrage complet N4 (séquence Navis)",
            "Ordre de démarrage documenté par Navis (GUIDE §1.10.9 p.457-458) et repris par le cahier des " +
            "charges §8.5. Attention : ce n'est pas l'inverse de la séquence d'arrêt — les Cluster Nodes " +
            "démarrent en premier.");

        // Étape 1 du cahier des charges §8.5 : le démarrage ne s'engage pas sans prérequis d'infrastructure.
        template.AddCheckpoint(
            "Contrôles infrastructure, base, réseau et dossier partagé",
            "Tous les prérequis critiques sont au vert.", "CDC §8.5 étape 1");

        // Premiers, et un par un : démarrer un second nœud avant que le premier soit ACTIVE et complètement
        // initialisé provoque des conflits de validation et des extensions de code dupliquées [GUIDE p.457].
        template.AddTier(
            N4ComponentKind.ClusterNode, "Cluster Nodes", SequenceTierExecution.Sequential,
            ClusterActiveCriteria, isOptional: false, null, "GUIDE p.457 §1.10.9-1/2 + p.458");

        template.AddTier(
            N4ComponentKind.CenterNode, "Center Node", SequenceTierExecution.Sequential,
            "Statut ACTIVE. Le démarrage peut prendre plusieurs minutes ; la vue Cluster Services ne se " +
            "rafraîchit complètement qu'une fois le Center Node actif.",
            isOptional: false, null, "GUIDE p.457 §1.10.9-4");

        template.AddTier(
            N4ComponentKind.StandbyCenterNode, "Standby Center Node", SequenceTierExecution.Sequential,
            "Statut « Initializing » dans Node Info Desk : c'est le comportement normal tant qu'il n'a pas " +
            "pris le relais du Center Node primaire.",
            isOptional: true, null, "GUIDE p.457 §1.10.9-5");

        template.AddTier(
            N4ComponentKind.XpsBridgeDaemon, "XPS Bridge Daemon", SequenceTierExecution.Sequential,
            "Daemon complètement démarré — condition impérative avant de lancer XPS.",
            isOptional: false, null, "GUIDE p.457 §1.10.9-6, p.464");

        template.AddTier(
            N4ComponentKind.XpsServer, "XPS", SequenceTierExecution.Sequential,
            "Les quatre sous-services XPS affichent ACTIVE.", isOptional: false, null,
            "GUIDE p.457 §1.10.9-7, p.465");

        template.AddTier(
            N4ComponentKind.Ecn4Daemon, "ECN4 (daemon)", SequenceTierExecution.Sequential,
            "Statut ACTIVE dans Cluster Services avant de lancer ECN4Web.", isOptional: true, null,
            "GUIDE p.457 §1.10.9-8a, p.467");

        template.AddTier(
            N4ComponentKind.Ecn4Web, "ECN4Web", SequenceTierExecution.Sequential,
            "Statut ACTIVE, et liaison ECN4/ECN4Web vérifiée.", isOptional: true, null,
            "GUIDE p.457 §1.10.9-8b, p.467");

        template.AddTier(
            N4ComponentKind.Billing, "N4 Billing", SequenceTierExecution.Sequential,
            "Service démarré. N'apparaît pas dans Cluster Services : Billing ne rejoint pas le cluster.",
            isOptional: true, null, "GUIDE p.457 §1.10.9-9");

        template.AddCheckpoint(
            "Tests de bout en bout",
            "Cluster Services, heartbeats, files, endpoints et test fonctionnel validés.",
            "CDC §8.5 étape 10 (FR-035, FR-037)");

        Publish(template);
        return template;
    }

    /// <summary>
    /// Fait franchir à la séquence livrée son cycle de validation complet. Les valeurs par défaut sont
    /// réputées validées à l'installation : elles proviennent de la documentation éditeur, pas d'une saisie.
    /// </summary>
    private static void Publish(SequenceTemplate template)
    {
        template.SubmitForValidation();
        template.Validate();
        template.Activate();
    }
}
