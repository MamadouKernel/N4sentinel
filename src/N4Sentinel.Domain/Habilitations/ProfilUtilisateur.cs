namespace N4Sentinel.Domain.Habilitations;

/// <summary>
/// §2.3.2 — les huit profils d'accès à la solution, dans l'ordre du cahier des charges.
/// Un utilisateur peut en cumuler plusieurs ; les droits s'additionnent, jamais ne se retranchent.
/// </summary>
public enum ProfilUtilisateur
{
    /// <summary>Consulte tableaux de bord, états, alertes, historiques, diagnostics, rapports, SOP et documentation.</summary>
    LecteurSupportN1,

    /// <summary>Lance des diagnostics, exécute les scénarios autorisés, confirme les étapes, reprend depuis un point de contrôle.</summary>
    OperateurN4SupportN2,

    /// <summary>Exécute les opérations sensibles, analyse les incidents avancés, maintient les règles N4, demande un contournement.</summary>
    AdministrateurN4,

    /// <summary>Configure connecteurs, contrôles réseau et système, certificats, comptes de service et références de secrets.</summary>
    AdministrateurInfrastructure,

    /// <summary>Approuve ou refuse opérations critiques, contournements, arrêts forcés et changements de niveau d'automatisation.</summary>
    ValidateurResponsableHabilite,

    /// <summary>Gère environnements, composants, workflows, seuils, règles, notifications, rôles, documents et paramètres.</summary>
    AdministrateurDeLaSolution,

    /// <summary>Accède en lecture aux historiques, preuves, rapports, versions, modifications, habilitations et journaux d'audit.</summary>
    Auditeur,

    /// <summary>Consulte les états, alertes et rapports d'intérêt opérationnel, sans pilotage ni administration technique.</summary>
    TosManagerConsultation
}
