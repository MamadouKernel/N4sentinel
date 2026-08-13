namespace N4Sentinel.Domain.Habilitations;

/// <summary>
/// SEC-002 — moindre privilège. Le §2.3.2 impose que lecture, diagnostic, exécution,
/// approbation et administration soient attribués séparément : cette énumération est
/// la granularité à laquelle les droits sont réellement vérifiés, les profils n'étant
/// que des regroupements nommés.
/// </summary>
public enum Droit
{
    // — Lecture —
    Consulter,
    ConsulterLeJournalDAudit,
    ConsulterLesHabilitations,

    // — Diagnostic —
    LancerUnDiagnostic,
    AnalyserUnIncidentAvance,
    ImporterDesLogs,

    // — Exécution —
    ExecuterUneOperationAutorisee,
    ExecuterUneOperationSensible,
    ConfirmerUneEtape,
    AjouterObservationOuPreuve,
    ReprendreUneOperation,
    DemanderUnContournement,

    // — Approbation —
    Approuver,

    // — Administration —
    ConfigurerLesConnecteurs,
    MaintenirLesReglesN4,
    GererLeReferentiel,
    GererLesRoles
}

/// <summary>Classement des droits entre consultation et action.</summary>
public static class Droits
{
    /// <summary>
    /// Droits de pure consultation. La distinction est structurante : en Production,
    /// consulter reste possible avec un profil global, agir exige une habilitation explicite
    /// sur l'environnement.
    /// </summary>
    public static readonly IReadOnlySet<Droit> Lecture = new HashSet<Droit>
    {
        Droit.Consulter,
        Droit.ConsulterLeJournalDAudit,
        Droit.ConsulterLesHabilitations
    };

    public static bool EstUneAction(Droit droit) => !Lecture.Contains(droit);
}
