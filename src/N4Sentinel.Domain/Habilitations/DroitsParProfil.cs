namespace N4Sentinel.Domain.Habilitations;

/// <summary>
/// Correspondance entre les huit profils du §2.3.2 et les droits élémentaires.
/// Chaque ligne se lit directement dans la colonne « Responsabilités et droits principaux »
/// du cahier des charges ; s'en écarter demanderait un arbitrage DSI, pas une décision de code.
/// </summary>
public static class DroitsParProfil
{
    private static readonly Dictionary<ProfilUtilisateur, HashSet<Droit>> Table = new()
    {
        // « Consulter les tableaux de bord, états, alertes, historiques, diagnostics, rapports,
        //   SOP et articles documentaires. Importer des logs uniquement si cette autorisation
        //   lui est attribuée. » — l'import de logs n'est donc pas dans le profil de base :
        //   il s'accorde séparément, par habilitation explicite.
        [ProfilUtilisateur.LecteurSupportN1] = Ensemble(
            Droit.Consulter),

        [ProfilUtilisateur.OperateurN4SupportN2] = Ensemble(
            Droit.Consulter,
            Droit.LancerUnDiagnostic,
            Droit.ImporterDesLogs,
            Droit.ExecuterUneOperationAutorisee,
            Droit.ConfirmerUneEtape,
            Droit.AjouterObservationOuPreuve,
            Droit.ReprendreUneOperation),

        [ProfilUtilisateur.AdministrateurN4] = Ensemble(
            Droit.Consulter,
            Droit.LancerUnDiagnostic,
            Droit.AnalyserUnIncidentAvance,
            Droit.ImporterDesLogs,
            Droit.ExecuterUneOperationAutorisee,
            Droit.ExecuterUneOperationSensible,
            Droit.ConfirmerUneEtape,
            Droit.AjouterObservationOuPreuve,
            Droit.ReprendreUneOperation,
            Droit.DemanderUnContournement,
            Droit.MaintenirLesReglesN4),

        [ProfilUtilisateur.AdministrateurInfrastructure] = Ensemble(
            Droit.Consulter,
            Droit.ConfigurerLesConnecteurs),

        [ProfilUtilisateur.ValidateurResponsableHabilite] = Ensemble(
            Droit.Consulter,
            Droit.Approuver,
            Droit.ConsulterLeJournalDAudit),

        [ProfilUtilisateur.AdministrateurDeLaSolution] = Ensemble(
            Droit.Consulter,
            Droit.GererLeReferentiel,
            Droit.GererLesRoles,
            Droit.ConsulterLeJournalDAudit),

        // « Accéder en lecture » — l'auditeur ne dispose d'aucun droit d'action.
        // Un auditeur qui pourrait agir ne serait plus un auditeur.
        [ProfilUtilisateur.Auditeur] = Ensemble(
            Droit.Consulter,
            Droit.ConsulterLeJournalDAudit,
            Droit.ConsulterLesHabilitations),

        // « sans droit de pilotage des services ni d'administration technique »
        [ProfilUtilisateur.TosManagerConsultation] = Ensemble(
            Droit.Consulter)
    };

    public static IReadOnlySet<Droit> Pour(ProfilUtilisateur profil) =>
        Table.TryGetValue(profil, out var droits) ? droits : Ensemble();

    public static IReadOnlySet<Droit> Pour(IEnumerable<ProfilUtilisateur> profils)
    {
        ArgumentNullException.ThrowIfNull(profils);

        var cumul = new HashSet<Droit>();
        foreach (var profil in profils)
        {
            cumul.UnionWith(Pour(profil));
        }

        return cumul;
    }

    /// <summary>Les huit profils du §2.3.2, dans l'ordre du cahier des charges.</summary>
    public static IReadOnlyList<ProfilUtilisateur> TousLesProfils { get; } =
        Enum.GetValues<ProfilUtilisateur>();

    private static HashSet<Droit> Ensemble(params Droit[] droits) => [.. droits];
}
