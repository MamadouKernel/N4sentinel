using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Connecteurs;
using N4Sentinel.Application.Supervision;
using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Supervision;

namespace N4Sentinel.Data.Supervision;

/// <summary>
/// FR-050 à FR-055 — construit la cartographie à partir des relevés stockés, et déclenche
/// la collecte quand on la lui demande.
/// </summary>
public sealed class ServiceDeSupervision(
    ApplicationDbContext contexte,
    IRepartiteurDeConnecteurs repartiteur,
    IClock horloge) : IServiceDeSupervision
{
    public async Task<CartographieDeSupervision?> LireAsync(
        Guid environnementId,
        CancellationToken cancellationToken = default)
    {
        var environnement = await contexte.Environnements
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == environnementId, cancellationToken);

        if (environnement is null)
        {
            return null;
        }

        var composants = await contexte.Composants
            .AsNoTracking()
            .Where(c => c.EnvironmentId == environnementId)
            .OrderBy(c => c.Kind)
            .ThenBy(c => c.Nom)
            .ToListAsync(cancellationToken);

        var identifiants = composants.Select(c => c.Id).ToList();

        // Deux relevés par composant et par type : le dernier pour l'état, l'avant-dernier
        // pour savoir si une file croît. Au-delà, l'historique ne sert pas cet écran.
        var releves = await contexte.Releves
            .AsNoTracking()
            .Where(r => r.ComposantCibleId != null && identifiants.Contains(r.ComposantCibleId.Value))
            .OrderByDescending(r => r.ReleveLe)
            .Take(identifiants.Count * 20)
            .ToListAsync(cancellationToken);

        var maintenant = horloge.MaintenantUtc;
        var lignes = new List<LigneDeSupervision>(composants.Count);
        var toutesLesAlertes = new List<Alerte>();

        foreach (var composant in composants)
        {
            var siens = releves.Where(r => r.ComposantCibleId == composant.Id).ToList();

            var derniersParType = siens
                .GroupBy(r => r.Type, StringComparer.Ordinal)
                .Select(g => g.OrderByDescending(r => r.ReleveLe).First())
                .ToList();

            var signaux = derniersParType.Select(Convertir).ToList();
            var transitions = derniersParType.Select(r => r.Transition).ToList();
            var derniereDonnee = derniersParType.Count == 0
                ? (DateTimeOffset?)null
                : derniersParType.Max(r => r.ReleveLe);

            var etat = EvaluationDeSupervision.Evaluer(
                composant.ModeDePilotage,
                composant.EnMaintenance,
                composant.Statut,
                signaux,
                transitions,
                derniereDonnee);

            var (precedente, courante) = LireLesTaillesDeFile(siens);

            var alertes = DetecteurDAlertes.Detecter(
                new ContexteDAlerte(
                    composant.Nom,
                    etat.Etat,
                    signaux,
                    derniereDonnee,
                    precedente,
                    courante,
                    composant.Criticite is Criticality.Haute or Criticality.Critique),
                maintenant);

            lignes.Add(new LigneDeSupervision(
                composant.Id,
                composant.Nom,
                composant.Kind,
                composant.Criticite,
                composant.ModeDePilotage,
                composant.Statut,
                etat,
                signaux,
                alertes));

            toutesLesAlertes.AddRange(alertes);
        }

        return new CartographieDeSupervision(
            environnement.Id,
            environnement.Nom,
            environnement.Type == EnvironmentType.Production,
            maintenant,
            lignes,
            [.. toutesLesAlertes.OrderByDescending(a => a.Critique).ThenBy(a => a.Motif)]);
    }

    public async Task<int> CollecterAsync(Guid environnementId, CancellationToken cancellationToken = default)
    {
        var composants = await contexte.Composants
            .AsNoTracking()
            .Include(c => c.Controles)
            .Include(c => c.Endpoints)
            .Where(c => c.EnvironmentId == environnementId
                        && c.ModeDePilotage != ModeDePilotage.NonSupervise)
            .ToListAsync(cancellationToken);

        var maintenant = horloge.MaintenantUtc;
        var enregistres = 0;

        foreach (var composant in composants)
        {
            foreach (var controle in composant.Controles.Where(c => c.Actif))
            {
                var demande = new DemandeDeCollecte(
                    controle.TypeDeControle,
                    CibleDe(controle, composant),
                    controle.Parametres,
                    PortDe(controle, composant),
                    TimeSpan.FromSeconds(controle.TimeoutSecondes));

                var signal = await repartiteur.CollecterAsync(demande, cancellationToken);

                contexte.Releves.Add(new ControlSignal
                {
                    EnvironmentId = environnementId,
                    ComposantCibleId = composant.Id,
                    Type = signal.Type,
                    Cible = demande.Cible,
                    Valeur = signal.Detail,
                    Verdict = signal.Verdict,
                    SuffitSeulAConclure = signal.SuffitSeulAConclure,
                    Transition = signal.Transition,
                    ReleveLe = maintenant,
                    Qualite = signal.Verdict switch
                    {
                        VerdictDeSignal.Indisponible => SignalQuality.Indisponible,
                        VerdictDeSignal.Perime => SignalQuality.Perime,
                        _ => SignalQuality.Fiable
                    },
                    MotifIndisponibilite = signal.Verdict == VerdictDeSignal.Indisponible
                        ? signal.Detail
                        : null
                });

                enregistres++;
            }
        }

        await contexte.SaveChangesAsync(cancellationToken);
        return enregistres;
    }

    private static SignalConsolidable Convertir(ControlSignal releve) =>
        new(releve.Type, releve.Verdict, releve.Valeur, releve.SuffitSeulAConclure, releve.Transition);

    /// <summary>
    /// Deux derniers relevés de file, s'il en existe. Sans deux points, une croissance ne
    /// s'observe pas : une seule mesure ne dit rien d'une tendance.
    /// </summary>
    private static (long? Precedente, long? Courante) LireLesTaillesDeFile(IEnumerable<ControlSignal> releves)
    {
        var mesures = releves
            .Where(r => r.Type.Contains("File", StringComparison.OrdinalIgnoreCase)
                        || r.Type.Contains("Queue", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r => r.ReleveLe)
            .Take(2)
            .Select(r => long.TryParse(r.Valeur, out var valeur) ? valeur : (long?)null)
            .ToList();

        return mesures.Count < 2 ? (null, null) : (mesures[1], mesures[0]);
    }

    private static string CibleDe(ComponentCheck controle, N4Component composant)
    {
        if (controle.TypeDeControle.Equals(TypesDeControle.ServiceWindows, StringComparison.OrdinalIgnoreCase))
        {
            return composant.NomDuService ?? controle.Parametres ?? composant.Nom;
        }

        if (!string.IsNullOrWhiteSpace(controle.Parametres)
            && (controle.Parametres.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                || controle.Parametres.StartsWith(@"\\", StringComparison.Ordinal)
                || Path.IsPathRooted(controle.Parametres)))
        {
            return controle.Parametres;
        }

        return composant.NomDns ?? composant.AdresseIp ?? composant.Serveur;
    }

    private static int? PortDe(ComponentCheck controle, N4Component composant)
    {
        if (int.TryParse(controle.Parametres, out var port) && port is > 0 and <= 65535)
        {
            return port;
        }

        return composant.Endpoints.FirstOrDefault(e => e.Port is > 0)?.Port;
    }
}
