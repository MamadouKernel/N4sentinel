using N4Sentinel.Domain.Execution;

namespace N4Sentinel.Application.Connecteurs;

/// <summary>
/// Ce qu'on demande à un connecteur de commande. Même garantie que <see cref="DemandeDeCollecte"/>
/// (Sprint 3) : aucun paramètre ne porte de secret, seule une référence de coffre circule.
/// </summary>
public sealed record DemandeDeCommande(
    string Action,
    string Cible,
    string? Parametres = null,
    TimeSpan? Timeout = null,
    string? ReferenceDeSecret = null);

/// <summary>Résultat d'une tentative de commande, preuve incluse — jamais de secret dedans.</summary>
public sealed record ResultatDExecutionDeCommande(
    ResultatDeCommande Resultat,
    string Preuve,
    string? MotifDEchec = null);

/// <summary>
/// Sprint 7 — le contrat d'écriture annoncé depuis le Sprint 3 : « les actions de pilotage
/// relèveront d'un autre contrat, avec ses propres garde-fous ». Volontairement séparé
/// d'<see cref="IConnecteurDeSignaux"/> — un connecteur qui saurait à la fois lire et écrire
/// finirait par être utilisé pour écrire depuis un écran de consultation.
///
/// Comme pour les connecteurs de lecture, <see cref="Action"/> ne circule jamais en texte
/// libre à ce niveau : elle est validée contre <see cref="ActionsDePilotage"/> à la saisie de
/// l'étape (SEC-006), bien avant d'atteindre un connecteur.
/// </summary>
public interface IConnecteurDeCommandes
{
    IReadOnlyCollection<string> ActionsPriseEnCharge { get; }

    Task<ResultatDExecutionDeCommande> ExecuterAsync(
        DemandeDeCommande demande,
        CancellationToken cancellationToken = default);
}

/// <summary>Aiguille une demande de commande vers le connecteur qui sait la traiter.</summary>
public interface IRepartiteurDeCommandes
{
    IReadOnlyCollection<string> ActionsPrisesEnCharge { get; }

    Task<ResultatDExecutionDeCommande> ExecuterAsync(
        DemandeDeCommande demande,
        CancellationToken cancellationToken = default);
}
