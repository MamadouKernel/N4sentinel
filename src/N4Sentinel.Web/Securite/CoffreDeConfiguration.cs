using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Web.Securite;

/// <summary>
/// SEC-003 — implémentation provisoire du coffre à secrets, adossée à la configuration du
/// serveur : variables d'environnement en production, secrets utilisateur en développement.
/// Aucune valeur ne vit dans le dépôt.
///
/// Ce n'est pas un coffre : il n'offre ni rotation, ni journal d'accès, ni cloisonnement. Il
/// tient le contrat en attendant que la DSI désigne la solution CIT — question ouverte du
/// dossier d'architecture. Le reste de l'application ne verra pas la différence : elle ne
/// manipule que des références.
/// </summary>
public sealed class CoffreDeConfiguration(IConfiguration configuration) : ISecretResolver
{
    private const string Section = "Secrets";

    public Task<string> ResoudreAsync(string referenceDeSecret, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceDeSecret);

        var valeur = configuration[$"{Section}:{referenceDeSecret}"];

        return string.IsNullOrEmpty(valeur)
            ? throw new InvalidOperationException(
                $"Référence de secret « {referenceDeSecret} » introuvable. "
                + "La valeur doit être fournie par la configuration du serveur, jamais par le dépôt.")
            : Task.FromResult(valeur);
    }

    public Task<bool> ExisteAsync(string referenceDeSecret, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceDeSecret);

        return Task.FromResult(!string.IsNullOrEmpty(configuration[$"{Section}:{referenceDeSecret}"]));
    }
}
