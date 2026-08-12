using System.Text.RegularExpressions;

namespace N4Sentinel.Domain.Common;

/// <summary>
/// SEC-003 — masquage des informations sensibles avant persistance et avant affichage.
///
/// La preuve d'une étape est la sortie brute d'une commande passée sur un serveur N4 : elle
/// contient parfois une chaîne de connexion, un jeton ou un mot de passe. Cette preuve est
/// ensuite écrite en base, relue à l'écran de suivi et jointe au dossier d'opération — trois
/// endroits où un secret n'a rien à faire. <c>ExecutionStep.Preuve</c> annonçait ce masquage
/// depuis le Sprint 0 sans que rien ne l'applique.
///
/// Le masquage est fait **avant** l'écriture, jamais seulement à l'affichage : un secret déjà
/// persisté est un secret divulgué, que l'écran le montre ou non.
///
/// Portée assumée : les motifs les plus courants d'affectation d'un secret, pas une détection
/// exhaustive. Un masquage partiel vaut mieux que pas de masquage, mais il ne dispense pas de
/// n'émettre que des actions du catalogue fermé (SEC-006), qui ne transportent aucun secret.
/// </summary>
public static partial class MasquageDesSecrets
{
    public const string Remplacement = "***";

    /// <summary>Texte masqué, secrets remplacés. Un texte vide ou nul ressort vide.</summary>
    public static string Appliquer(string? texte) => AppliquerEtCompter(texte).Texte;

    /// <summary>
    /// Même masquage, en indiquant combien de secrets ont été trouvés — le compte est ce qui
    /// permet de dire « aucun secret détecté » plutôt que « rien n'a été vérifié ».
    /// </summary>
    public static (string Texte, int NombreMasque) AppliquerEtCompter(string? texte)
    {
        if (string.IsNullOrEmpty(texte))
        {
            return (string.Empty, 0);
        }

        var compte = 0;
        var resultat = texte;

        foreach (var motif in Motifs)
        {
            resultat = motif.Replace(resultat, correspondance =>
            {
                compte++;
                return correspondance.Groups["prefixe"].Value + Remplacement;
            });
        }

        return (resultat, compte);
    }

    private static IEnumerable<Regex> Motifs
    {
        get
        {
            yield return Affectation();
            yield return JetonPorteur();
        }
    }

    /// <summary>
    /// <c>clé = valeur</c> où la clé désigne un secret. La valeur s'arrête au premier séparateur
    /// — espace, point-virgule ou virgule — pour ne masquer que le secret et laisser lisible le
    /// reste d'une chaîne de connexion, qui est souvent la partie utile au diagnostic.
    /// </summary>
    [GeneratedRegex(
        """(?<prefixe>\b(?:password|pwd|passwd|mot\s*de\s*passe|secret|token|api[_-]?key|apikey)\b\s*[:=]\s*)(?:"[^"]*"|'[^']*'|[^\s;,"']+)""",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 200)]
    private static partial Regex Affectation();

    /// <summary>En-tête <c>Authorization: Bearer …</c>, dont le jeton n'est précédé d'aucune clé.</summary>
    [GeneratedRegex(
        """(?<prefixe>\bBearer\s+)[A-Za-z0-9\-._~+/]+=*""",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 200)]
    private static partial Regex JetonPorteur();
}
