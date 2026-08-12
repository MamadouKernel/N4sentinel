using N4Sentinel.Domain.Common;

namespace N4Sentinel.Domain.Referentiel;

/// <summary>
/// Contrat du fichier de configuration des scripts d'exploitation N4 (SOP-2,
/// <c>Navis-Config.json</c>). Les noms de propriétés reprennent ceux du fichier : c'est un
/// format existant, en service, que l'application lit — pas un format qu'elle impose.
///
/// Le fichier décrit une topologie par **rôle** : un hôte pour le Center, un pour le Standby,
/// une liste pour les Cluster Nodes, et un nom de service Windows par rôle. Deux rôles peuvent
/// partager un hôte — Bridge et XPS le font couramment — et deux rôles peuvent partager un nom
/// de service : le Standby exécute le même service que le Center, ce qui est précisément
/// pourquoi un service démarré ne suffit pas à dire lequel détient le rôle actif.
/// </summary>
public sealed record ConfigurationN4(
    string? CenterNode,
    string? StandbyNode,
    IReadOnlyList<string>? ClusterNodes,
    string? BridgeHost,
    string? XPSHost,
    string? ECN4Host,
    NomsDeServicesN4? ServiceNames,
    string? SharedFolder,
    string? DatabaseHost,
    int? DatabasePort);

/// <summary>Nom du service Windows portant chaque rôle, tel que déclaré par l'exploitation.</summary>
public sealed record NomsDeServicesN4(
    string? Center,
    string? Cluster,
    string? Standby,
    string? Bridge,
    string? XPS,
    string? ECN4,
    string? ECN4Web);

/// <summary>Un composant tel que la topologie le décrit, avant écriture au référentiel.</summary>
/// <param name="Nom">Désignation lisible, unique dans l'environnement.</param>
/// <param name="Serveur">Hôte qui l'exécute.</param>
/// <param name="NomDuService">Service Windows, quand le composant en a un.</param>
/// <param name="TimeoutSecondes">Délai normal d'arrêt, propre au rôle.</param>
public sealed record ComposantDeTopologie(
    string Nom,
    N4ComponentKind Kind,
    string Serveur,
    string? NomDuService,
    ModeDePilotage ModeDePilotage,
    Criticality Criticite,
    int TimeoutSecondes);

/// <summary>Ce qui a été compris du fichier, et ce qui manquait pour aller plus loin.</summary>
public sealed record LectureDeTopologie(
    IReadOnlyList<ComposantDeTopologie> Composants,
    IReadOnlyList<string> Anomalies)
{
    public bool Exploitable => Composants.Count > 0;
}

/// <summary>
/// Traduit une configuration de scripts en composants du référentiel.
///
/// Rien n'est deviné : un rôle dont l'hôte ou le service manque n'est pas créé « au cas où »,
/// il est signalé. Un composant à moitié décrit produirait une étape d'arrêt qui échouerait en
/// pleine séquence, ce qui est très exactement ce que le référentiel existe pour éviter.
///
/// Les délais viennent du terrain plutôt que d'une valeur par défaut : les scripts arrêtent un
/// service en 90 secondes, mais un Cluster Node qui ne quitte pas proprement le cluster fait
/// attendre le timeout Hazelcast de dix minutes — le forcer avant serait le tuer pendant qu'il
/// se retire.
/// </summary>
public static class LecteurDeTopologieN4
{
    public const int DelaiDArretParDefautSecondes = 90;
    public const int DelaiDArretDUnClusterNodeSecondes = 600;

    public static LectureDeTopologie Lire(ConfigurationN4? configuration)
    {
        var composants = new List<ComposantDeTopologie>();
        var anomalies = new List<string>();

        if (configuration is null)
        {
            return new LectureDeTopologie([], ["Configuration illisible ou vide."]);
        }

        var services = configuration.ServiceNames;

        Ajouter(composants, anomalies, "Center Node", N4ComponentKind.CenterNode,
            configuration.CenterNode, services?.Center, Criticality.Critique);

        Ajouter(composants, anomalies, "Standby Center Node", N4ComponentKind.StandbyCenterNode,
            configuration.StandbyNode, services?.Standby, Criticality.Haute);

        LireLesClusterNodes(configuration, services, composants, anomalies);

        Ajouter(composants, anomalies, "XPS Bridge Daemon", N4ComponentKind.BridgeDaemon,
            configuration.BridgeHost, services?.Bridge, Criticality.Haute);

        Ajouter(composants, anomalies, "XPS", N4ComponentKind.Xps,
            configuration.XPSHost, services?.XPS, Criticality.Haute);

        Ajouter(composants, anomalies, "ECN4", N4ComponentKind.Ecn4,
            configuration.ECN4Host, services?.ECN4, Criticality.Moyenne);

        Ajouter(composants, anomalies, "ECN4 Web", N4ComponentKind.Ecn4Web,
            configuration.ECN4Host, services?.ECN4Web, Criticality.Moyenne);

        LireLaBaseDeDonnees(configuration, composants, anomalies);

        return new LectureDeTopologie(composants, anomalies);
    }

    private static void LireLesClusterNodes(
        ConfigurationN4 configuration,
        NomsDeServicesN4? services,
        List<ComposantDeTopologie> composants,
        List<string> anomalies)
    {
        var hotes = configuration.ClusterNodes ?? [];

        if (hotes.Count == 0)
        {
            anomalies.Add("Aucun Cluster Node déclaré : la séquence d'arrêt serait incomplète.");
            return;
        }

        if (string.IsNullOrWhiteSpace(services?.Cluster))
        {
            anomalies.Add("Nom du service des Cluster Nodes absent : aucun n'a été repris.");
            return;
        }

        // Un nom par hôte : l'ordre d'arrêt se joue nœud par nœud, chacun devant être confirmé
        // avant le suivant. Un composant unique « les Cluster Nodes » ne le permettrait pas.
        foreach (var hote in hotes.Where(h => !string.IsNullOrWhiteSpace(h)))
        {
            composants.Add(new ComposantDeTopologie(
                $"Cluster Node {hote}",
                N4ComponentKind.ClusterNode,
                hote,
                services.Cluster,
                ModeDePilotage.Pilotable,
                Criticality.Haute,
                DelaiDArretDUnClusterNodeSecondes));
        }
    }

    /// <summary>
    /// La base est reprise en supervision seule. Le fichier le dit lui-même : seule la
    /// connectivité réseau est vérifiée, toute investigation approfondie restant au DBA. Un
    /// composant que l'application n'a pas le droit d'arrêter ne doit pas être déclaré pilotable.
    /// </summary>
    private static void LireLaBaseDeDonnees(
        ConfigurationN4 configuration,
        List<ComposantDeTopologie> composants,
        List<string> anomalies)
    {
        if (string.IsNullOrWhiteSpace(configuration.DatabaseHost))
        {
            anomalies.Add("Hôte de base de données absent : la base n'a pas été reprise.");
            return;
        }

        composants.Add(new ComposantDeTopologie(
            "Base de données N4",
            N4ComponentKind.BaseDeDonnees,
            configuration.DatabaseHost,
            NomDuService: null,
            ModeDePilotage.UniquementSupervise,
            Criticality.Critique,
            DelaiDArretParDefautSecondes));
    }

    private static void Ajouter(
        List<ComposantDeTopologie> composants,
        List<string> anomalies,
        string nom,
        N4ComponentKind kind,
        string? hote,
        string? service,
        Criticality criticite)
    {
        if (string.IsNullOrWhiteSpace(hote) && string.IsNullOrWhiteSpace(service))
        {
            anomalies.Add($"{nom} : ni hôte ni service déclarés, rôle absent de cette topologie.");
            return;
        }

        if (string.IsNullOrWhiteSpace(hote))
        {
            anomalies.Add($"{nom} : service « {service} » déclaré sans hôte, composant non repris.");
            return;
        }

        if (string.IsNullOrWhiteSpace(service))
        {
            anomalies.Add($"{nom} : hôte « {hote} » déclaré sans nom de service, composant non repris.");
            return;
        }

        composants.Add(new ComposantDeTopologie(
            nom, kind, hote, service, ModeDePilotage.Pilotable, criticite,
            DelaiDArretParDefautSecondes));
    }
}
