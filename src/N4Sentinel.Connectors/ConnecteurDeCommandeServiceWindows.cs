using System.Runtime.Versioning;
using System.ServiceProcess;
using N4Sentinel.Application.Connecteurs;
using N4Sentinel.Domain.Execution;

namespace N4Sentinel.Connectors;

/// <summary>
/// Sprint 7 — arrêt, démarrage et redémarrage réels d'un service Windows. Même syntaxe de
/// cible que le connecteur de lecture du Sprint 3 (<c>machine\service</c>), même prudence :
/// aucune commande arbitraire, seul un nom de service est accepté.
///
/// L'arrêt forcé (<see cref="ActionsDePilotage.ArreterServiceWindowsDeForce"/>) ne cherche pas
/// le PID du service par lui-même : il arrête le processus dont le nom correspond à la cible.
/// C'est une simplification assumée — un service hébergé par un processus partagé (svchost)
/// n'y répondrait pas correctement — documentée en limite plutôt que masquée derrière une
/// résolution WMI non vérifiée.
///
/// Non exercé contre un vrai service dans la vérification de ce sprint : par prudence sur le
/// poste de développement, comme le connecteur SQL du Sprint 3 n'a jamais tourné contre une
/// vraie base.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class ConnecteurDeCommandeServiceWindows : IConnecteurDeCommandes
{
    private static readonly TimeSpan DelaiParDefaut = TimeSpan.FromSeconds(30);

    public IReadOnlyCollection<string> ActionsPriseEnCharge { get; } =
    [
        ActionsDePilotage.ArreterServiceWindows,
        ActionsDePilotage.DemarrerServiceWindows,
        ActionsDePilotage.RedemarrerServiceWindows,
        ActionsDePilotage.ArreterServiceWindowsDeForce
    ];

    public Task<ResultatDExecutionDeCommande> ExecuterAsync(
        DemandeDeCommande demande,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(demande);

        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult(new ResultatDExecutionDeCommande(
                ResultatDeCommande.Echouee, "Système non Windows.", "Système non Windows."));
        }

        var resultat = demande.Action switch
        {
            ActionsDePilotage.ArreterServiceWindows => Arreter(demande),
            ActionsDePilotage.DemarrerServiceWindows => Demarrer(demande),
            ActionsDePilotage.RedemarrerServiceWindows => Redemarrer(demande),
            ActionsDePilotage.ArreterServiceWindowsDeForce => ArreterDeForce(demande),
            _ => new ResultatDExecutionDeCommande(
                ResultatDeCommande.NonSupportee, "Action non prise en charge par ce connecteur.")
        };

        return Task.FromResult(resultat);
    }

    private static ResultatDExecutionDeCommande Arreter(DemandeDeCommande demande)
    {
        var (machine, service) = Decouper(demande.Cible);
        var delai = demande.Timeout ?? DelaiParDefaut;

        try
        {
            using var controleur = Ouvrir(machine, service);

            if (controleur.Status == ServiceControllerStatus.Stopped)
            {
                return new ResultatDExecutionDeCommande(ResultatDeCommande.Reussie, $"{service} déjà arrêté.");
            }

            if (!controleur.CanStop)
            {
                return new ResultatDExecutionDeCommande(
                    ResultatDeCommande.Echouee, $"{service} ne peut pas être arrêté (CanStop=false).", "CanStop=false");
            }

            controleur.Stop();
            controleur.WaitForStatus(ServiceControllerStatus.Stopped, delai);

            return new ResultatDExecutionDeCommande(ResultatDeCommande.Reussie, $"{service} arrêté.");
        }
        catch (System.ServiceProcess.TimeoutException)
        {
            return new ResultatDExecutionDeCommande(
                ResultatDeCommande.EnCours,
                $"{service} n'a pas confirmé son arrêt sous {delai.TotalSeconds:F0} s : toujours en StopPending.");
        }
        catch (InvalidOperationException erreur)
        {
            return new ResultatDExecutionDeCommande(ResultatDeCommande.Echouee, $"{service} — {erreur.Message}", erreur.Message);
        }
    }

    private static ResultatDExecutionDeCommande Demarrer(DemandeDeCommande demande)
    {
        var (machine, service) = Decouper(demande.Cible);
        var delai = demande.Timeout ?? DelaiParDefaut;

        try
        {
            using var controleur = Ouvrir(machine, service);

            if (controleur.Status == ServiceControllerStatus.Running)
            {
                return new ResultatDExecutionDeCommande(ResultatDeCommande.Reussie, $"{service} déjà démarré.");
            }

            controleur.Start();
            controleur.WaitForStatus(ServiceControllerStatus.Running, delai);

            return new ResultatDExecutionDeCommande(ResultatDeCommande.Reussie, $"{service} démarré.");
        }
        catch (System.ServiceProcess.TimeoutException)
        {
            return new ResultatDExecutionDeCommande(
                ResultatDeCommande.EnCours,
                $"{service} n'a pas confirmé son démarrage sous {delai.TotalSeconds:F0} s : toujours en StartPending.");
        }
        catch (InvalidOperationException erreur)
        {
            return new ResultatDExecutionDeCommande(ResultatDeCommande.Echouee, $"{service} — {erreur.Message}", erreur.Message);
        }
    }

    private static ResultatDExecutionDeCommande Redemarrer(DemandeDeCommande demande)
    {
        var arret = Arreter(demande);
        if (arret.Resultat != ResultatDeCommande.Reussie)
        {
            return arret;
        }

        return Demarrer(demande);
    }

    private static ResultatDExecutionDeCommande ArreterDeForce(DemandeDeCommande demande)
    {
        var (_, nomDeProcessus) = Decouper(demande.Cible);
        return UtilitairesDeProcessus.ArreterParNom(nomDeProcessus);
    }

    private static ServiceController Ouvrir(string? machine, string service) =>
        machine is null ? new ServiceController(service) : new ServiceController(service, machine);

    private static (string? Machine, string Service) Decouper(string cible)
    {
        var separateur = cible.IndexOf('\\', StringComparison.Ordinal);

        return separateur <= 0
            ? (null, cible)
            : (cible[..separateur], cible[(separateur + 1)..]);
    }
}
