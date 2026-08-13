using System.Diagnostics;
using System.Globalization;
using N4Sentinel.Application.Connecteurs;
using N4Sentinel.Domain.Execution;

namespace N4Sentinel.Connectors.Tests;

/// <summary>
/// Sprint 7 — même discipline que <see cref="ConnecteurTcpTests"/> et consorts : une vraie
/// ressource locale, jamais un service ou processus système. « choice.exe », utilitaire console
/// standard de Windows, sert de cible jetable — nom peu susceptible d'entrer en collision avec un
/// processus réel du poste, sans fenêtre, sans effet de bord au-delà de sa propre durée de vie.
/// Le connecteur de service Windows, lui, n'est délibérément pas exercé ici en arrêt/démarrage
/// réel — même prudence que le connecteur SQL du Sprint 3, jamais lancé contre une vraie base.
/// </summary>
public class ConnecteurDeCommandeProcessusTests
{
    private static Process LancerUnProcessusJetable()
    {
        var demarrage = new ProcessStartInfo("choice.exe", "/T 120 /D Y")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardInput = true
        };

        return Process.Start(demarrage)!;
    }

    [Fact]
    public async Task Arreter_par_pid_termine_reellement_le_processus()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using var processus = LancerUnProcessusJetable();
        Assert.False(processus.HasExited);

        var resultat = await new ConnecteurDeCommandeProcessus().ExecuterAsync(
            new DemandeDeCommande(ActionsDePilotage.ArreterProcessus, processus.Id.ToString(CultureInfo.InvariantCulture)),
            TestContext.Current.CancellationToken);

        Assert.Equal(ResultatDeCommande.Reussie, resultat.Resultat);
        Assert.True(processus.WaitForExit(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task Arreter_par_nom_termine_toutes_les_instances()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using var premier = LancerUnProcessusJetable();
        using var second = LancerUnProcessusJetable();

        var resultat = await new ConnecteurDeCommandeProcessus().ExecuterAsync(
            new DemandeDeCommande(ActionsDePilotage.ArreterProcessus, "choice"),
            TestContext.Current.CancellationToken);

        Assert.Equal(ResultatDeCommande.Reussie, resultat.Resultat);
        Assert.True(premier.WaitForExit(TimeSpan.FromSeconds(5)));
        Assert.True(second.WaitForExit(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task Un_pid_deja_arrete_est_traite_comme_deja_arrete()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        int pid;
        using (var processus = LancerUnProcessusJetable())
        {
            pid = processus.Id;
            processus.Kill();
            processus.WaitForExit();
        }

        var resultat = await new ConnecteurDeCommandeProcessus().ExecuterAsync(
            new DemandeDeCommande(ActionsDePilotage.ArreterProcessus, pid.ToString(CultureInfo.InvariantCulture)),
            TestContext.Current.CancellationToken);

        Assert.Equal(ResultatDeCommande.Reussie, resultat.Resultat);
        Assert.Contains("déjà arrêté", resultat.Preuve, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Une_action_hors_catalogue_n_est_pas_prise_en_charge()
    {
        var resultat = await new ConnecteurDeCommandeProcessus().ExecuterAsync(
            new DemandeDeCommande("ActionInconnue", "1234"),
            TestContext.Current.CancellationToken);

        Assert.Equal(ResultatDeCommande.NonSupportee, resultat.Resultat);
    }

    [Fact]
    public void Le_catalogue_ne_declare_qu_une_seule_action()
    {
        var actions = new ConnecteurDeCommandeProcessus().ActionsPriseEnCharge;

        Assert.Single(actions);
        Assert.Contains(ActionsDePilotage.ArreterProcessus, actions);
    }
}
