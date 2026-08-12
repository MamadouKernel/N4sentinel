using System.ComponentModel;
using System.Diagnostics;
using N4Sentinel.Application.Connecteurs;
using N4Sentinel.Domain.Execution;

namespace N4Sentinel.Connectors;

/// <summary>
/// Arrêt d'un processus par PID ou par nom, partagé par <see cref="ConnecteurDeCommandeProcessus"/>
/// (Standby Center Node « par gestionnaire de tâches ») et l'arrêt forcé du connecteur de
/// service Windows. Jamais de ligne de commande arbitraire : seuls un identifiant de processus
/// ou un nom sont acceptés (SEC-006).
/// </summary>
internal static class UtilitairesDeProcessus
{
    public static ResultatDExecutionDeCommande ArreterParPid(int pid)
    {
        try
        {
            using var processus = Process.GetProcessById(pid);
            return Arreter(processus);
        }
        catch (ArgumentException)
        {
            // Aucun processus vivant avec ce PID : c'est le résultat attendu d'un arrêt.
            return new ResultatDExecutionDeCommande(
                ResultatDeCommande.Reussie, $"Aucun processus actif avec le PID {pid} : déjà arrêté.");
        }
    }

    public static ResultatDExecutionDeCommande ArreterParNom(string nom)
    {
        var processus = Process.GetProcessesByName(nom);
        if (processus.Length == 0)
        {
            return new ResultatDExecutionDeCommande(
                ResultatDeCommande.Reussie, $"Aucun processus nommé « {nom} » : déjà arrêté.");
        }

        var erreurs = new List<string>();
        foreach (var p in processus)
        {
            var resultat = Arreter(p);
            if (resultat.Resultat != ResultatDeCommande.Reussie)
            {
                erreurs.Add($"PID {p.Id} — {resultat.MotifDEchec}");
            }
        }

        return erreurs.Count == 0
            ? new ResultatDExecutionDeCommande(ResultatDeCommande.Reussie, $"{processus.Length} processus « {nom} » arrêté(s).")
            : new ResultatDExecutionDeCommande(
                ResultatDeCommande.Echouee, string.Join(" ; ", erreurs), string.Join(" ; ", erreurs));
    }

    private static ResultatDExecutionDeCommande Arreter(Process processus)
    {
        try
        {
            var pid = processus.Id;
            processus.Kill(entireProcessTree: true);
            var arrete = processus.WaitForExit((int)TimeSpan.FromSeconds(10).TotalMilliseconds);

            return arrete
                ? new ResultatDExecutionDeCommande(ResultatDeCommande.Reussie, $"Processus {pid} arrêté.")
                : new ResultatDExecutionDeCommande(
                    ResultatDeCommande.EnCours, $"Processus {pid} : signal envoyé, arrêt non confirmé sous 10 s.");
        }
        catch (Exception erreur) when (erreur is InvalidOperationException or Win32Exception)
        {
            // Déjà sorti entre la lecture et l'arrêt, ou droits insuffisants : deux cas
            // différents, mais aucun des deux ne doit interrompre l'appelant.
            return new ResultatDExecutionDeCommande(
                ResultatDeCommande.Echouee, $"Processus {processus.Id} — {erreur.Message}", erreur.Message);
        }
        finally
        {
            processus.Dispose();
        }
    }
}
