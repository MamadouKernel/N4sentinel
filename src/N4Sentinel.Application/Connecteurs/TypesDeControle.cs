namespace N4Sentinel.Application.Connecteurs;

/// <summary>
/// Types de contrôle reconnus. Ils appartiennent au contrat et non aux connecteurs :
/// le référentiel les saisit, le testeur les interprète, les connecteurs les servent.
/// </summary>
public static class TypesDeControle
{
    public const string ServiceWindows = "ServiceWindows";
    public const string PortTcp = "PortTcp";
    public const string EndpointHttp = "EndpointHttp";
    public const string DossierPartage = "DossierPartage";
    public const string RequeteSqlLectureSeule = "RequeteSqlLectureSeule";
    public const string ClusterServices = "ClusterServices";

    /// <summary>
    /// SOP-3 — écart entre l'horloge d'un serveur et celle du serveur applicatif. Contrôle
    /// quotidien du corpus, et Top 10 des causes de P1 selon Kaleris : un écart fausse
    /// silencieusement les statuts relus, dont dépend chaque décision du moteur.
    /// </summary>
    public const string EcartDHorloge = "EcartDHorloge";

    public static IReadOnlyList<string> Tous { get; } =
    [
        ServiceWindows, PortTcp, EndpointHttp, DossierPartage, RequeteSqlLectureSeule,
        ClusterServices, EcartDHorloge
    ];
}
