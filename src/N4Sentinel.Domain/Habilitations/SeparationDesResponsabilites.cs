using N4Sentinel.Domain.Common;

namespace N4Sentinel.Domain.Habilitations;

/// <summary>Verdict d'un contrôle d'habilitation, motivé pour pouvoir être audité tel quel.</summary>
public sealed record VerdictDHabilitation(bool Autorise, string Motif)
{
    public static VerdictDHabilitation Accorde() => new(true, "Autorisé.");

    public static VerdictDHabilitation Refuse(string motif) => new(false, motif);
}

/// <summary>
/// §2.3.2 — règles de séparation des responsabilités. Elles sont ici, dans le domaine, et non
/// dans une page : une règle de séparation qui ne vit que dans l'interface n'en est pas une.
/// </summary>
public static class SeparationDesResponsabilites
{
    /// <summary>
    /// « Le lancement et l'approbation d'une même opération critique en Production doivent être
    /// réalisés par deux utilisateurs distincts lorsque la double validation est requise. »
    /// </summary>
    public static VerdictDHabilitation PeutApprouverUneOperation(
        string demandeurId,
        string approbateurId,
        IReadOnlySet<Droit> droitsDeLApprobateur,
        EnvironmentType typeDEnvironnement,
        bool doubleValidationRequise)
    {
        ArgumentNullException.ThrowIfNull(droitsDeLApprobateur);

        if (!droitsDeLApprobateur.Contains(Droit.Approuver))
        {
            return VerdictDHabilitation.Refuse(
                "L'utilisateur ne dispose pas du droit d'approbation sur cet environnement.");
        }

        var separationExigee = doubleValidationRequise || typeDEnvironnement == EnvironmentType.Production;

        if (separationExigee && string.Equals(demandeurId, approbateurId, StringComparison.Ordinal))
        {
            return VerdictDHabilitation.Refuse(
                "Le demandeur d'une opération ne peut pas l'approuver lui-même.");
        }

        return VerdictDHabilitation.Accorde();
    }

    /// <summary>
    /// « Un Administrateur N4 peut demander un contournement, mais ne doit pas approuver seul
    /// son propre contournement. » La règle vaut quel que soit l'environnement : un contournement
    /// est par nature une sortie du cadre validé.
    /// </summary>
    public static VerdictDHabilitation PeutApprouverUnContournement(
        string demandeurId,
        string approbateurId,
        IReadOnlySet<Droit> droitsDeLApprobateur)
    {
        ArgumentNullException.ThrowIfNull(droitsDeLApprobateur);

        if (!droitsDeLApprobateur.Contains(Droit.Approuver))
        {
            return VerdictDHabilitation.Refuse(
                "L'utilisateur ne dispose pas du droit d'approbation sur cet environnement.");
        }

        if (string.Equals(demandeurId, approbateurId, StringComparison.Ordinal))
        {
            return VerdictDHabilitation.Refuse(
                "Le demandeur d'un contournement ne peut pas approuver son propre contournement.");
        }

        return VerdictDHabilitation.Accorde();
    }

    /// <summary>
    /// « Toute attribution, modification ou révocation de rôle doit être autorisée et auditée. »
    /// Le contrôle d'autorisation est ici ; la trace d'audit est posée par l'appelant, qui seul
    /// connaît la valeur avant et après.
    /// </summary>
    public static VerdictDHabilitation PeutModifierLesHabilitations(
        IReadOnlySet<Droit> droitsDeLActeur)
    {
        ArgumentNullException.ThrowIfNull(droitsDeLActeur);

        return droitsDeLActeur.Contains(Droit.GererLesRoles)
            ? VerdictDHabilitation.Accorde()
            : VerdictDHabilitation.Refuse("La gestion des habilitations exige le droit correspondant.");
    }
}
