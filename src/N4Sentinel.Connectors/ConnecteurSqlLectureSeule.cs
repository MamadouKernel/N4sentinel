using System.Globalization;
using Microsoft.Data.SqlClient;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Connecteurs;
using N4Sentinel.Domain.Supervision;

namespace N4Sentinel.Connectors;

/// <summary>
/// SQL Server en lecture seule.
///
/// SEC-006 — « l'utilisateur sélectionne des actions approuvées ; aucune console libre n'est
/// exposée ». Le contrôle ne transporte donc pas de requête : il nomme un contrôle du
/// catalogue ci-dessous, et le connecteur choisit lui-même le texte SQL. Une requête arrivant
/// du référentiel serait, de fait, une console libre à retardement.
///
/// SEC-003 — la chaîne de connexion est assemblée ici à partir d'une référence de coffre.
/// Le mot de passe n'existe qu'en mémoire, le temps de l'appel, et n'est jamais journalisé.
/// </summary>
public sealed class ConnecteurSqlLectureSeule(ISecretResolver coffre) : IConnecteurDeSignaux
{
    /// <summary>Catalogue des contrôles approuvés. Aucun n'écrit, aucun ne verrouille.</summary>
    private static readonly Dictionary<string, (string Sql, string Libelle)> Catalogue = new(
        StringComparer.OrdinalIgnoreCase)
    {
        ["Disponibilite"] = ("SELECT 1;", "disponibilité"),

        ["Sessions"] = (
            "SELECT COUNT(*) FROM sys.dm_exec_sessions WHERE is_user_process = 1;",
            "sessions utilisateur"),

        ["Verrous"] = (
            "SELECT COUNT(*) FROM sys.dm_tran_locks WHERE request_status = 'WAIT';",
            "verrous en attente"),

        ["RequetesLentes"] = (
            "SELECT COUNT(*) FROM sys.dm_exec_requests "
            + "WHERE total_elapsed_time > 30000 AND session_id <> @@SPID;",
            "requêtes de plus de 30 s")
    };

    public string TypeDeControle => TypesDeControle.RequeteSqlLectureSeule;

    public async Task<SignalConsolidable> CollecterAsync(
        DemandeDeCollecte demande,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(demande);

        var nomDuControle = demande.Parametres ?? "Disponibilite";

        if (!Catalogue.TryGetValue(nomDuControle, out var controle))
        {
            return Indisponible(
                $"Contrôle « {nomDuControle} » absent du catalogue approuvé "
                + $"({string.Join(", ", Catalogue.Keys)}).");
        }

        string chaine;
        try
        {
            chaine = await AssemblerLaChaineAsync(demande, cancellationToken);
        }
        catch (InvalidOperationException erreur)
        {
            return Indisponible(erreur.Message);
        }

        try
        {
            await using var connexion = new SqlConnection(chaine);
            await connexion.OpenAsync(cancellationToken);

            await using var commande = connexion.CreateCommand();
            commande.CommandText = controle.Sql;
            commande.CommandTimeout = (int)(demande.Timeout ?? TimeSpan.FromSeconds(10)).TotalSeconds;

            var valeur = await commande.ExecuteScalarAsync(cancellationToken);
            var lu = Convert.ToInt64(valeur ?? 0L, CultureInfo.InvariantCulture);

            // Un compteur au-delà du seuil est dégradé, pas défavorable : la base répond,
            // c'est son état qui interpelle.
            var verdict = nomDuControle.Equals("Disponibilite", StringComparison.OrdinalIgnoreCase) || lu == 0
                ? VerdictDeSignal.Favorable
                : VerdictDeSignal.Degrade;

            return new SignalConsolidable(
                TypesDeControle.RequeteSqlLectureSeule,
                verdict,
                $"{demande.Cible} — {controle.Libelle} : {lu.ToString(CultureInfo.InvariantCulture)}");
        }
        catch (SqlException erreur)
        {
            return new SignalConsolidable(
                TypesDeControle.RequeteSqlLectureSeule,
                VerdictDeSignal.Defavorable,
                $"{demande.Cible} — erreur SQL {erreur.Number}");
        }
        catch (InvalidOperationException erreur)
        {
            return Indisponible($"{demande.Cible} — {erreur.Message}");
        }
    }

    private async Task<string> AssemblerLaChaineAsync(
        DemandeDeCollecte demande,
        CancellationToken cancellationToken)
    {
        var constructeur = new SqlConnectionStringBuilder
        {
            DataSource = demande.Cible,
            InitialCatalog = "master",
            TrustServerCertificate = true,
            ConnectTimeout = (int)(demande.Timeout ?? TimeSpan.FromSeconds(10)).TotalSeconds,

            // Lecture seule affirmée jusqu'au serveur : sur une base à réplicas, la session
            // est routée vers un réplica en lecture. Une écriture y échouerait.
            ApplicationIntent = ApplicationIntent.ReadOnly,
            ApplicationName = "N4 Sentinel (lecture seule)"
        };

        if (string.IsNullOrWhiteSpace(demande.ReferenceDeSecret))
        {
            constructeur.IntegratedSecurity = true;
            return constructeur.ConnectionString;
        }

        // La référence désigne un couple « utilisateur:référenceDuMotDePasse ».
        var parties = demande.ReferenceDeSecret.Split(':', 2);
        if (parties.Length != 2)
        {
            throw new InvalidOperationException(
                "Référence de secret attendue au format « utilisateur:referenceDuMotDePasse ».");
        }

        constructeur.UserID = parties[0];
        constructeur.Password = await coffre.ResoudreAsync(parties[1], cancellationToken);

        return constructeur.ConnectionString;
    }

    private static SignalConsolidable Indisponible(string motif) =>
        new(TypesDeControle.RequeteSqlLectureSeule, VerdictDeSignal.Indisponible, motif);
}
