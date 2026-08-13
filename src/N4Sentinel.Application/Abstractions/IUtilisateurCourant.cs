namespace N4Sentinel.Application.Abstractions;

/// <summary>
/// Utilisateur à l'origine de la requête en cours. Toute écriture de piste d'audit passe par
/// cette abstraction : une action tracée sous un acteur inconnu ne vaut pas trace.
/// </summary>
public interface IUtilisateurCourant
{
    bool EstAuthentifie { get; }

    string? Identifiant { get; }

    string NomAffiche { get; }

    string? AdresseIp { get; }
}
