namespace N4Sentinel.Application.Abstractions;

/// <summary>
/// SEC-001 — le second facteur d'authentification est envoyé par courriel. Le contrat est
/// volontairement minimal : l'application n'a pas à connaître le transport retenu par CIT.
/// </summary>
public interface IEnvoiDeCourriel
{
    Task EnvoyerAsync(
        string destinataire,
        string objet,
        string corps,
        CancellationToken cancellationToken = default);
}
