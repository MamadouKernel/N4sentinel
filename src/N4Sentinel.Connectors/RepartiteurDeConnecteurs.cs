using N4Sentinel.Application.Connecteurs;
using N4Sentinel.Domain.Supervision;

namespace N4Sentinel.Connectors;

/// <summary>
/// Aiguille chaque demande vers le connecteur qui déclare prendre en charge son type.
///
/// Un type inconnu ne produit pas d'erreur : il produit un signal indisponible motivé.
/// Faire échouer la collecte entière parce qu'un contrôle sur dix n'est pas reconnu priverait
/// l'exploitant des neuf autres.
/// </summary>
public sealed class RepartiteurDeConnecteurs : IRepartiteurDeConnecteurs
{
    private readonly Dictionary<string, IConnecteurDeSignaux> connecteurs;

    public RepartiteurDeConnecteurs(IEnumerable<IConnecteurDeSignaux> connecteurs)
    {
        ArgumentNullException.ThrowIfNull(connecteurs);

        this.connecteurs = connecteurs.ToDictionary(
            c => c.TypeDeControle,
            StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<string> TypesPrisEnCharge => [.. connecteurs.Keys.Order(StringComparer.Ordinal)];

    public Task<SignalConsolidable> CollecterAsync(
        DemandeDeCollecte demande,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(demande);

        if (!connecteurs.TryGetValue(demande.TypeDeControle, out var connecteur))
        {
            return Task.FromResult(new SignalConsolidable(
                demande.TypeDeControle,
                VerdictDeSignal.Indisponible,
                $"Aucun connecteur ne prend en charge le type « {demande.TypeDeControle} ». "
                + $"Types disponibles : {string.Join(", ", TypesPrisEnCharge)}."));
        }

        return connecteur.CollecterAsync(demande, cancellationToken);
    }
}
