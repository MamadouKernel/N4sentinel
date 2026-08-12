using N4Sentinel.Application.Connecteurs;
using N4Sentinel.Domain.Execution;

namespace N4Sentinel.Connectors;

/// <summary>
/// Sprint 7 — pendant en écriture de <see cref="RepartiteurDeConnecteurs"/> : aiguille chaque
/// demande vers le connecteur qui déclare prendre en charge son action. Une action inconnue ne
/// lève jamais d'exception : elle produit un résultat non pris en charge, motivé.
/// </summary>
public sealed class RepartiteurDeCommandes : IRepartiteurDeCommandes
{
    private readonly Dictionary<string, IConnecteurDeCommandes> connecteurs;

    public RepartiteurDeCommandes(IEnumerable<IConnecteurDeCommandes> connecteurs)
    {
        ArgumentNullException.ThrowIfNull(connecteurs);

        this.connecteurs = new Dictionary<string, IConnecteurDeCommandes>(StringComparer.OrdinalIgnoreCase);
        foreach (var connecteur in connecteurs)
        {
            foreach (var action in connecteur.ActionsPriseEnCharge)
            {
                this.connecteurs[action] = connecteur;
            }
        }
    }

    public IReadOnlyCollection<string> ActionsPrisesEnCharge => [.. connecteurs.Keys.Order(StringComparer.Ordinal)];

    public Task<ResultatDExecutionDeCommande> ExecuterAsync(
        DemandeDeCommande demande,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(demande);

        if (!connecteurs.TryGetValue(demande.Action, out var connecteur))
        {
            var motif = $"Aucun connecteur ne prend en charge l'action « {demande.Action} ». "
                + $"Actions disponibles : {string.Join(", ", ActionsPrisesEnCharge)}.";

            return Task.FromResult(new ResultatDExecutionDeCommande(ResultatDeCommande.NonSupportee, motif, motif));
        }

        return connecteur.ExecuterAsync(demande, cancellationToken);
    }
}
