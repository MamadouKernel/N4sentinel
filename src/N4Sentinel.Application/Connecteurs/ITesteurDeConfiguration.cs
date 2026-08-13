using N4Sentinel.Domain.Supervision;

namespace N4Sentinel.Application.Connecteurs;

/// <summary>Résultat du test de configuration d'un composant.</summary>
/// <param name="ComposantId">Composant testé.</param>
/// <param name="Signaux">Signaux collectés, un par contrôle déclaré.</param>
/// <param name="Etat">État consolidé qui en découle.</param>
/// <param name="ControlesDeclares">Nombre de contrôles déclarés au référentiel.</param>
public sealed record ResultatDuTest(
    Guid ComposantId,
    IReadOnlyList<SignalConsolidable> Signaux,
    EtatConsolide Etat,
    int ControlesDeclares);

/// <summary>
/// FR-007 — « tester, sans action mutative, la connectivité des composants, l'accès aux
/// connecteurs, la disponibilité des services et la validité des contrôles configurés avant
/// l'activation d'un environnement ou d'un workflow ».
///
/// « Sans action mutative » n'est pas une intention : c'est une propriété du contrat. Ce
/// service n'a accès qu'à <see cref="IConnecteurDeSignaux"/>, qui ne sait que lire.
/// </summary>
public interface ITesteurDeConfiguration
{
    Task<ResultatDuTest> TesterLeComposantAsync(
        Guid composantId,
        CancellationToken cancellationToken = default);
}
