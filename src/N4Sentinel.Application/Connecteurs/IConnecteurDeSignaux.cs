using N4Sentinel.Domain.Supervision;

namespace N4Sentinel.Application.Connecteurs;

/// <summary>
/// Ce qu'on demande à un connecteur. Aucun paramètre ne porte de secret : seule une référence
/// de coffre circule, résolue au moment de l'appel (SEC-003).
/// </summary>
/// <param name="TypeDeControle">Type déclaré sur le contrôle du référentiel.</param>
/// <param name="Cible">Ce qu'il faut interroger : nom de service, hôte, chemin, requête nommée.</param>
/// <param name="Parametres">Paramètres du contrôle, tels que saisis au référentiel.</param>
/// <param name="Port">Port, lorsque le contrôle en vise un.</param>
/// <param name="Timeout">Au-delà, le signal est déclaré indisponible plutôt qu'attendu indéfiniment.</param>
/// <param name="ReferenceDeSecret">Référence de coffre, jamais une valeur.</param>
public sealed record DemandeDeCollecte(
    string TypeDeControle,
    string Cible,
    string? Parametres = null,
    int? Port = null,
    TimeSpan? Timeout = null,
    string? ReferenceDeSecret = null);

/// <summary>
/// §3.10.2 — collecte d'un signal sur un composant.
///
/// Le contrat est délibérément en lecture seule : aucune méthode ne permet d'agir. Les actions
/// de pilotage relèveront d'un autre contrat, avec ses propres garde-fous — un connecteur qui
/// saurait à la fois lire et écrire finirait par être utilisé pour écrire depuis un écran de
/// consultation.
/// </summary>
public interface IConnecteurDeSignaux
{
    /// <summary>Type de contrôle pris en charge, tel qu'il est saisi au référentiel.</summary>
    string TypeDeControle { get; }

    /// <summary>
    /// Collecte le signal. N'échoue jamais par exception attendue : une cible injoignable
    /// produit un signal <see cref="VerdictDeSignal.Indisponible"/> motivé, car c'est une
    /// information à afficher, pas une erreur à avaler.
    /// </summary>
    Task<SignalConsolidable> CollecterAsync(
        DemandeDeCollecte demande,
        CancellationToken cancellationToken = default);
}

/// <summary>Aiguille une demande vers le connecteur qui sait la traiter.</summary>
public interface IRepartiteurDeConnecteurs
{
    IReadOnlyCollection<string> TypesPrisEnCharge { get; }

    Task<SignalConsolidable> CollecterAsync(
        DemandeDeCollecte demande,
        CancellationToken cancellationToken = default);
}
