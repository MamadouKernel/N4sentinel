namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Nature technique d'un composant N4, indépendante de son libellé. Sans ce typage, le champ libre
/// <see cref="N4Component.Role"/> ne permet à aucune règle de savoir qu'un composant *est* un Cluster Node :
/// l'ordre d'arrêt/démarrage ne serait alors qu'une convention humaine, non vérifiable.
///
/// Les valeurs reprennent les composants réels documentés par Navis — noms de services Windows
/// <c>[GUIDE p.454]</c> et diagramme d'architecture <c>[ITADMIN p.5-6]</c> — plutôt qu'une taxonomie inventée.
/// </summary>
public enum N4ComponentKind
{
    /// <summary>Non typé : composant hérité ou hors périmètre de séquencement.</summary>
    Unspecified = 0,

    /// <summary>Service « Navis N4 Cluster Node ». Plusieurs par environnement.</summary>
    ClusterNode = 1,

    /// <summary>Service « Navis N4 Center Node » sur l'hôte Center.</summary>
    CenterNode = 2,

    /// <summary>Même service que le Center Node, sur l'hôte de bascule dédié.</summary>
    StandbyCenterNode = 3,

    /// <summary>Service « Navis XPS Bridge Daemon ».</summary>
    XpsBridgeDaemon = 4,

    /// <summary>Service « Navis XPS ».</summary>
    XpsServer = 5,

    /// <summary>Service « Navis ECN4 Daemon » (conditionnel selon licence Equipment Control).</summary>
    Ecn4Daemon = 6,

    /// <summary>Service « Navis ECN4Web » (conditionnel selon licence Equipment Control).</summary>
    Ecn4Web = 7,

    /// <summary>Service « Navis N4 Billing » (conditionnel selon licence).</summary>
    Billing = 8,

    /// <summary>Base de données N4 (SQL Server ou Oracle).</summary>
    Database = 9,

    /// <summary>Broker de messages : ActiveMQ, ou Kafka en variante haute disponibilité.</summary>
    MessageBroker = 10,

    /// <summary>Service « Navis LogCollector Tool ».</summary>
    LogCollector = 11,

    /// <summary>Poste client XPS (salle serveur ou utilisateur).</summary>
    XpsClient = 12,
}
