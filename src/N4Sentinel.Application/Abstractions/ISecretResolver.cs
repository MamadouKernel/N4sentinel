namespace N4Sentinel.Application.Abstractions;

/// <summary>
/// SEC-003 — les comptes techniques vivent dans un coffre. L'application ne manipule que des
/// références de secret ; la valeur n'est résolue qu'au moment de l'appel connecteur, et n'est
/// jamais retournée vers l'interface, ni journalisée, ni persistée.
/// </summary>
public interface ISecretResolver
{
    /// <summary>Résout une référence de secret en valeur utilisable, le temps d'un appel.</summary>
    Task<string> ResoudreAsync(string referenceDeSecret, CancellationToken cancellationToken = default);

    /// <summary>Indique si la référence est déclarée dans le coffre, sans en révéler la valeur.</summary>
    Task<bool> ExisteAsync(string referenceDeSecret, CancellationToken cancellationToken = default);
}
