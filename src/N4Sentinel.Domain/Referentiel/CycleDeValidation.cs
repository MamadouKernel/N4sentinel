using N4Sentinel.Domain.Common;

namespace N4Sentinel.Domain.Referentiel;

/// <summary>
/// FR-006 — cycle Brouillon → À valider → Validé → Actif → Désactivé.
///
/// Les transitions sont énumérées plutôt que laissées libres : sans cela, « Actif » ne
/// garantirait rien, puisqu'on pourrait y arriver depuis n'importe où. FR-002 s'appuie sur
/// cette garantie — « aucune action technique sur un composant qui n'a pas été préalablement
/// enregistré et validé ».
/// </summary>
public static class CycleDeValidation
{
    private static readonly Dictionary<ValidationStatus, ValidationStatus[]> Transitions = new()
    {
        [ValidationStatus.Brouillon] = [ValidationStatus.EnAttenteValidation],

        // Un refus de validation renvoie au brouillon : le rédacteur reprend son texte.
        [ValidationStatus.EnAttenteValidation] = [ValidationStatus.Valide, ValidationStatus.Brouillon],

        // Valider n'active pas. L'activation est un second geste, volontaire : c'est elle qui
        // rend l'objet utilisable en exploitation.
        [ValidationStatus.Valide] = [ValidationStatus.Actif, ValidationStatus.Brouillon],

        [ValidationStatus.Actif] = [ValidationStatus.Desactive],

        // Un objet désactivé repart en brouillon : le remettre en service impose de repasser
        // par la validation, car ce qui a justifié sa désactivation peut ne plus être vrai.
        [ValidationStatus.Desactive] = [ValidationStatus.Brouillon]
    };

    public static IReadOnlyList<ValidationStatus> TransitionsPossiblesDepuis(ValidationStatus statut) =>
        Transitions.TryGetValue(statut, out var suivants) ? suivants : [];

    public static bool EstAutorisee(ValidationStatus depuis, ValidationStatus vers) =>
        TransitionsPossiblesDepuis(depuis).Contains(vers);

    /// <summary>
    /// FR-002 et FR-006 — seul un objet actif peut servir à une opération. Un composant validé
    /// mais non activé n'est pas utilisable : la validation atteste du contenu, l'activation
    /// engage l'exploitation.
    /// </summary>
    public static bool EstUtilisablePourUneOperation(ValidationStatus statut) =>
        statut == ValidationStatus.Actif;

    public static string Libelle(ValidationStatus statut) => statut switch
    {
        ValidationStatus.Brouillon => "Brouillon",
        ValidationStatus.EnAttenteValidation => "À valider",
        ValidationStatus.Valide => "Validé",
        ValidationStatus.Actif => "Actif",
        ValidationStatus.Desactive => "Désactivé",
        _ => statut.ToString()
    };
}
