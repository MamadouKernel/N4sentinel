using System.Globalization;
using System.Runtime.Versioning;
using Microsoft.Management.Infrastructure;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Connecteurs;
using N4Sentinel.Domain.Supervision;

namespace N4Sentinel.Connectors;

/// <summary>
/// SOP-3 — écart d'horloge entre un serveur N4 et le serveur applicatif, contrôle quotidien du
/// corpus et Top 10 des causes de P1 identifiées par Navis/Kaleris.
///
/// Pourquoi ce signal compte ici plus qu'ailleurs : le moteur d'orchestration décide sur des
/// états relus — il saute une étape dont la cible est déjà dans l'état visé, conclut une étape
/// sur l'effet constaté. Un écart d'horloge « fausse silencieusement les statuts affichés : un
/// nœud actif peut apparaître DISCONNECTED ». Il ne dégrade donc pas un affichage, il corrompt
/// la donnée d'entrée de chaque décision d'arrêt ou de démarrage.
///
/// **Requête CIM typée, jamais un script.** Les scripts d'exploitation obtiennent la même
/// mesure par <c>Invoke-Command { Get-Date }</c>. L'application emprunte le même transport —
/// WinRM, déjà prérequis de ces scripts — mais interroge une classe et une propriété nommées.
/// La différence n'est pas stylistique : <c>SEC-006</c> interdit toute console libre, et le nom
/// d'hôte vient du référentiel, modifiable depuis l'IHM. Interpolé dans un script, il devient un
/// vecteur d'injection ; passé à une session CIM, ce n'est qu'une cible qui répond ou pas.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class ConnecteurDHorloge(IClock horloge) : IConnecteurDeSignaux
{
    private const string Classe = "Win32_OperatingSystem";
    private const string Propriete = "LocalDateTime";
    private const string EspaceDeNoms = @"root\cimv2";

    public string TypeDeControle => TypesDeControle.EcartDHorloge;

    public Task<SignalConsolidable> CollecterAsync(
        DemandeDeCollecte demande,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(demande);

        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult(Indisponible("Système non Windows."));
        }

        if (string.IsNullOrWhiteSpace(demande.Cible))
        {
            return Task.FromResult(Indisponible("Aucun hôte à interroger."));
        }

        try
        {
            var heureDistante = LireLHeure(demande.Cible, cancellationToken);

            if (heureDistante is null)
            {
                return Task.FromResult(Indisponible(
                    $"{demande.Cible} n'a retourné aucune heure système."));
            }

            var ecart = heureDistante.Value - horloge.MaintenantUtc;
            var verdict = SynchronisationDesHorloges.Evaluer(
                [new EcartDHorloge(demande.Cible, ecart)]);

            return Task.FromResult(new SignalConsolidable(
                TypeDeControle,
                verdict.Synchronisees ? VerdictDeSignal.Favorable : VerdictDeSignal.Degrade,
                $"Écart de {ecart.TotalSeconds:0.###} s avec {demande.Cible}. {verdict.Motif}",
                // Une horloge décalée ne dit rien de l'état du composant : elle dit que les
                // autres signaux sont douteux. Elle ne peut donc pas conclure seule.
                SuffitSeulAConclure: false));
        }
#pragma warning disable CA1031 // Une cible injoignable est une information, pas une erreur à propager.
        catch (Exception erreur)
        {
            return Task.FromResult(Indisponible(
                $"Horloge de {demande.Cible} illisible : {erreur.Message}"));
        }
#pragma warning restore CA1031
    }

    /// <summary>
    /// Interroge l'heure système de l'hôte. Le format CIM <c>DATETIME</c> porte le décalage
    /// horaire du serveur distant en minutes ; il est ramené en UTC, sans quoi deux serveurs
    /// correctement synchronisés mais dans des fuseaux différents paraîtraient décalés.
    /// </summary>
    private static DateTimeOffset? LireLHeure(string hote, CancellationToken cancellationToken)
    {
        // Session locale sans transport réseau quand la cible est la machine elle-même :
        // c'est ce qui rend le connecteur exerçable en développement, comme les cinq autres.
        using var session = EstLocal(hote)
            ? CimSession.Create(null)
            : CimSession.Create(hote);

        foreach (var instance in session.EnumerateInstances(EspaceDeNoms, Classe))
        {
            using (instance)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var valeur = instance.CimInstanceProperties[Propriete]?.Value;

                return valeur switch
                {
                    DateTime date => new DateTimeOffset(date.ToUniversalTime(), TimeSpan.Zero),
                    string texte => AnalyserLeFormatCim(texte),
                    _ => null
                };
            }
        }

        return null;
    }

    /// <summary>
    /// Format CIM <c>yyyyMMddHHmmss.ffffff±UUU</c>, où <c>UUU</c> est le décalage en minutes.
    /// </summary>
    private static DateTimeOffset? AnalyserLeFormatCim(string texte)
    {
        if (texte.Length < 22)
        {
            return null;
        }

        var partieLocale = texte[..14];
        var signe = texte[21];
        var minutes = texte[22..];

        if (!DateTime.TryParseExact(partieLocale, "yyyyMMddHHmmss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var locale)
            || !int.TryParse(minutes, NumberStyles.Integer, CultureInfo.InvariantCulture, out var decalage))
        {
            return null;
        }

        var offset = TimeSpan.FromMinutes(signe == '-' ? -decalage : decalage);

        return new DateTimeOffset(locale, offset).ToUniversalTime();
    }

    private static bool EstLocal(string hote) =>
        hote is "." or "localhost" or "127.0.0.1"
        || string.Equals(hote, Environment.MachineName, StringComparison.OrdinalIgnoreCase);

    private SignalConsolidable Indisponible(string motif) =>
        new(TypeDeControle, VerdictDeSignal.Indisponible, motif, SuffitSeulAConclure: false);
}
