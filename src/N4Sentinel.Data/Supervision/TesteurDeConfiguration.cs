using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Connecteurs;
using N4Sentinel.Domain.Supervision;

namespace N4Sentinel.Data.Supervision;

/// <summary>
/// FR-007 — teste la configuration d'un composant sans aucune action mutative.
///
/// Le service n'a qu'une dépendance de collecte, <see cref="IRepartiteurDeConnecteurs"/>, qui
/// ne sait que lire. L'absence d'effet de bord n'est donc pas une promesse tenue par
/// discipline : elle découle de ce que ce code peut atteindre.
/// </summary>
public sealed class TesteurDeConfiguration(
    ApplicationDbContext contexte,
    IRepartiteurDeConnecteurs repartiteur) : ITesteurDeConfiguration
{
    public async Task<ResultatDuTest> TesterLeComposantAsync(
        Guid composantId,
        CancellationToken cancellationToken = default)
    {
        var composant = await contexte.Composants
            .AsNoTracking()
            .Include(c => c.Controles)
            .Include(c => c.Endpoints)
            .FirstOrDefaultAsync(c => c.Id == composantId, cancellationToken);

        if (composant is null)
        {
            return new ResultatDuTest(
                composantId,
                [],
                new EtatConsolide(
                    Domain.Common.ComponentHealth.Inconnu,
                    "Composant introuvable au référentiel.",
                    []),
                0);
        }

        var controlesActifs = composant.Controles.Where(c => c.Actif).ToList();
        var signaux = new List<SignalConsolidable>(controlesActifs.Count);

        foreach (var controle in controlesActifs)
        {
            // La cible est l'hôte du composant, sauf pour les contrôles qui portent la leur
            // dans leurs paramètres — chemin de dossier, adresse d'endpoint.
            var demande = new DemandeDeCollecte(
                controle.TypeDeControle,
                CibleDe(controle, composant),
                controle.Parametres,
                PortDe(controle, composant),
                TimeSpan.FromSeconds(controle.TimeoutSecondes));

            signaux.Add(await repartiteur.CollecterAsync(demande, cancellationToken));
        }

        if (controlesActifs.Count == 0)
        {
            // Un composant sans contrôle n'est pas un composant en bonne santé : c'est un
            // composant dont personne ne peut rien dire.
            return new ResultatDuTest(
                composantId,
                [],
                new EtatConsolide(
                    Domain.Common.ComponentHealth.Inconnu,
                    "Aucun contrôle de santé actif n'est déclaré sur ce composant.",
                    []),
                0);
        }

        return new ResultatDuTest(
            composantId,
            signaux,
            ConsolidationDEtat.Consolider(signaux),
            controlesActifs.Count);
    }

    private static string CibleDe(Domain.Entities.ComponentCheck controle, Domain.Entities.N4Component composant)
    {
        // Un contrôle de service vise un service nommé, jamais un hôte : c'est le nom déclaré
        // au référentiel qui fait foi, et à défaut celui saisi dans les paramètres.
        if (controle.TypeDeControle.Equals(TypesDeControle.ServiceWindows, StringComparison.OrdinalIgnoreCase))
        {
            return composant.NomDuService ?? controle.Parametres ?? composant.Nom;
        }

        // Un contrôle dont les paramètres portent un chemin ou une adresse vise cette cible-là ;
        // sinon, il vise l'hôte du composant.
        if (!string.IsNullOrWhiteSpace(controle.Parametres)
            && (controle.Parametres.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                || controle.Parametres.StartsWith(@"\\", StringComparison.Ordinal)
                || Path.IsPathRooted(controle.Parametres)))
        {
            return controle.Parametres;
        }

        return composant.NomDns ?? composant.AdresseIp ?? composant.Serveur;
    }

    private static int? PortDe(Domain.Entities.ComponentCheck controle, Domain.Entities.N4Component composant)
    {
        if (int.TryParse(controle.Parametres, out var port) && port is > 0 and <= 65535)
        {
            return port;
        }

        return composant.Endpoints.FirstOrDefault(e => e.Port is > 0)?.Port;
    }
}
