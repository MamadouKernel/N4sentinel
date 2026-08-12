using System.Globalization;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.ServiceProcess;
using N4Sentinel.Application.Connecteurs;
using N4Sentinel.Domain.Supervision;

namespace N4Sentinel.Connectors;

/// <summary>
/// SEC-006 — état d'un service Windows, sans console libre : le connecteur n'expose que la
/// lecture d'un service nommé, il ne lance aucune commande arbitraire.
///
/// Le verdict est marqué comme ne suffisant pas à conclure seul : FR-016 est explicite,
/// « un service déclaré Running ne suffit pas ».
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class ConnecteurDeServiceWindows : IConnecteurDeSignaux
{
    public string TypeDeControle => TypesDeControle.ServiceWindows;

    public Task<SignalConsolidable> CollecterAsync(
        DemandeDeCollecte demande,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(demande);

        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult(Indisponible("Système non Windows."));
        }

        try
        {
            // Le nom de machine éventuel est séparé du nom de service par « \ », comme dans
            // les outils d'administration Windows.
            var (machine, service) = Decouper(demande.Cible);

            using var controleur = machine is null
                ? new ServiceController(service)
                : new ServiceController(service, machine);

            var statut = controleur.Status;

            var verdict = statut switch
            {
                ServiceControllerStatus.Running => VerdictDeSignal.Favorable,
                ServiceControllerStatus.StartPending => VerdictDeSignal.Degrade,
                ServiceControllerStatus.ContinuePending => VerdictDeSignal.Degrade,
                ServiceControllerStatus.PausePending => VerdictDeSignal.Degrade,
                ServiceControllerStatus.Paused => VerdictDeSignal.Degrade,
                ServiceControllerStatus.StopPending => VerdictDeSignal.Degrade,
                _ => VerdictDeSignal.Defavorable
            };

            // Un service en attente de démarrage ou d'arrêt n'est ni haut ni bas : la
            // transition est constatée ici, seul endroit où l'information existe.
            var transition = statut switch
            {
                ServiceControllerStatus.StartPending => TransitionObservee.Demarrage,
                ServiceControllerStatus.ContinuePending => TransitionObservee.Demarrage,
                ServiceControllerStatus.StopPending => TransitionObservee.Arret,
                _ => TransitionObservee.Aucune
            };

            return Task.FromResult(new SignalConsolidable(
                TypesDeControle.ServiceWindows,
                verdict,
                $"{service} : {statut}",
                SuffitSeulAConclure: false,
                transition));
        }
        catch (InvalidOperationException erreur)
        {
            // Service inexistant, machine injoignable, droits insuffisants : autant de cas
            // où l'on ne sait pas — et où l'on ne doit surtout pas conclure « pas d'anomalie ».
            return Task.FromResult(Indisponible(erreur.Message));
        }
    }

    private static (string? Machine, string Service) Decouper(string cible)
    {
        var separateur = cible.IndexOf('\\', StringComparison.Ordinal);

        return separateur <= 0
            ? (null, cible)
            : (cible[..separateur], cible[(separateur + 1)..]);
    }

    private static SignalConsolidable Indisponible(string motif) =>
        new(TypesDeControle.ServiceWindows, VerdictDeSignal.Indisponible, motif, SuffitSeulAConclure: false);
}

/// <summary>Ouverture d'un port TCP. Lecture pure : la connexion est établie puis refermée.</summary>
public sealed class ConnecteurTcp : IConnecteurDeSignaux
{
    public string TypeDeControle => TypesDeControle.PortTcp;

    public async Task<SignalConsolidable> CollecterAsync(
        DemandeDeCollecte demande,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(demande);

        if (demande.Port is not > 0)
        {
            return new SignalConsolidable(
                TypesDeControle.PortTcp, VerdictDeSignal.Indisponible, "Port non renseigné.");
        }

        var delai = demande.Timeout ?? TimeSpan.FromSeconds(5);
        var debut = DateTimeOffset.UtcNow;

        try
        {
            using var client = new TcpClient();
            using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            limite.CancelAfter(delai);

            await client.ConnectAsync(demande.Cible, demande.Port.Value, limite.Token);

            var latence = (DateTimeOffset.UtcNow - debut).TotalMilliseconds;

            return new SignalConsolidable(
                TypesDeControle.PortTcp,
                VerdictDeSignal.Favorable,
                $"{demande.Cible}:{demande.Port} ouvert en {latence.ToString("F0", CultureInfo.InvariantCulture)} ms");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Un délai dépassé n'est pas un port fermé : on ne sait pas.
            return new SignalConsolidable(
                TypesDeControle.PortTcp,
                VerdictDeSignal.Indisponible,
                $"Aucune réponse de {demande.Cible}:{demande.Port} en {delai.TotalSeconds} s.");
        }
        catch (SocketException erreur)
        {
            return new SignalConsolidable(
                TypesDeControle.PortTcp,
                VerdictDeSignal.Defavorable,
                $"{demande.Cible}:{demande.Port} — {erreur.SocketErrorCode}");
        }
    }
}

/// <summary>
/// Disponibilité d'un endpoint HTTP. Uniquement des requêtes GET : aucune méthode susceptible
/// de modifier l'état du composant interrogé n'est émise.
/// </summary>
public sealed class ConnecteurHttp(HttpClient client) : IConnecteurDeSignaux
{
    public string TypeDeControle => TypesDeControle.EndpointHttp;

    public async Task<SignalConsolidable> CollecterAsync(
        DemandeDeCollecte demande,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(demande);

        if (!Uri.TryCreate(demande.Cible, UriKind.Absolute, out var adresse))
        {
            return new SignalConsolidable(
                TypesDeControle.EndpointHttp, VerdictDeSignal.Indisponible, "Adresse invalide.");
        }

        var debut = DateTimeOffset.UtcNow;

        try
        {
            using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            limite.CancelAfter(demande.Timeout ?? TimeSpan.FromSeconds(10));

            using var requete = new HttpRequestMessage(HttpMethod.Get, adresse);
            using var reponse = await client.SendAsync(requete, limite.Token);

            var latence = (DateTimeOffset.UtcNow - debut).TotalMilliseconds;
            var code = (int)reponse.StatusCode;

            var verdict = code switch
            {
                >= 200 and < 300 => VerdictDeSignal.Favorable,
                >= 300 and < 500 => VerdictDeSignal.Degrade,
                _ => VerdictDeSignal.Defavorable
            };

            return new SignalConsolidable(
                TypesDeControle.EndpointHttp,
                verdict,
                $"{adresse} → {code} en {latence.ToString("F0", CultureInfo.InvariantCulture)} ms");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new SignalConsolidable(
                TypesDeControle.EndpointHttp, VerdictDeSignal.Indisponible, $"{adresse} n'a pas répondu.");
        }
        catch (HttpRequestException erreur)
        {
            return new SignalConsolidable(
                TypesDeControle.EndpointHttp, VerdictDeSignal.Defavorable, $"{adresse} — {erreur.Message}");
        }
    }
}

/// <summary>
/// Accessibilité d'un dossier partagé, chemin UNC compris. Lecture seule : le connecteur
/// énumère, il n'écrit ni ne supprime rien.
/// </summary>
public sealed class ConnecteurDeDossier : IConnecteurDeSignaux
{
    public string TypeDeControle => TypesDeControle.DossierPartage;

    public Task<SignalConsolidable> CollecterAsync(
        DemandeDeCollecte demande,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(demande);

        try
        {
            if (!Directory.Exists(demande.Cible))
            {
                return Task.FromResult(new SignalConsolidable(
                    TypesDeControle.DossierPartage,
                    VerdictDeSignal.Defavorable,
                    $"{demande.Cible} est introuvable."));
            }

            var fichiers = Directory.EnumerateFiles(demande.Cible).Take(1).Count();

            return Task.FromResult(new SignalConsolidable(
                TypesDeControle.DossierPartage,
                VerdictDeSignal.Favorable,
                $"{demande.Cible} accessible en lecture{(fichiers == 0 ? ", vide" : string.Empty)}"));
        }
        catch (Exception erreur) when (erreur is UnauthorizedAccessException or IOException)
        {
            // Droits insuffisants ou partage injoignable : une absence de réponse, pas un verdict.
            return Task.FromResult(new SignalConsolidable(
                TypesDeControle.DossierPartage,
                VerdictDeSignal.Indisponible,
                $"{demande.Cible} — {erreur.Message}"));
        }
    }
}
