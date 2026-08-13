namespace N4Sentinel.Domain.Operations;

/// <summary>
/// Sprint 6 — circuit d'approbation d'une version de workflow. Un paramètre du workflow,
/// jamais une décision prise au moment de l'exécution — même philosophie que
/// <see cref="Orchestration.PolitiqueDeTransition"/> pour le contournement d'un contrôle bloquant.
/// </summary>
public enum TypeDeCircuitDApprobation
{
    /// <summary>Aucune approbation requise avant l'engagement (Sprint 7).</summary>
    Aucun,

    /// <summary>Un approbateur, distinct du demandeur.</summary>
    Simple,

    /// <summary>Deux approbateurs distincts, chacun distinct du demandeur.</summary>
    Doublee
}

/// <summary>Décision individuelle d'un approbateur sur une exécution.</summary>
public enum DecisionDApprobation
{
    Approuvee,
    Refusee
}

/// <summary>Verdict sur l'état d'avancement d'un circuit d'approbation.</summary>
public sealed record VerdictDeCircuit(bool Complet, string Motif);

/// <summary>
/// FR-013 — circuit d'approbation configurable, simple ou double, avec approbateurs distincts.
///
/// Ne décide jamais qu'un individu donné peut approuver — c'est le rôle de
/// <see cref="Habilitations.SeparationDesResponsabilites.PeutApprouverUneOperation"/>, appelé
/// avant chaque tentative d'ajout. Cette classe se contente de compter les approbations déjà
/// accordées par des acteurs distincts et de dire si le circuit est complet.
/// </summary>
public static class EvaluateurDeCircuit
{
    public static VerdictDeCircuit Evaluer(
        TypeDeCircuitDApprobation type,
        IReadOnlyCollection<string> approbateursDistincts)
    {
        ArgumentNullException.ThrowIfNull(approbateursDistincts);

        var distincts = approbateursDistincts
            .Distinct(StringComparer.Ordinal)
            .Count();

        return type switch
        {
            TypeDeCircuitDApprobation.Aucun =>
                new VerdictDeCircuit(true, "Aucune approbation requise par la version validée."),

            TypeDeCircuitDApprobation.Simple => distincts >= 1
                ? new VerdictDeCircuit(true, "Approbation reçue.")
                : new VerdictDeCircuit(false, "Une approbation est requise."),

            TypeDeCircuitDApprobation.Doublee => distincts >= 2
                ? new VerdictDeCircuit(true, "Deux approbations distinctes reçues.")
                : new VerdictDeCircuit(false,
                    $"Deux approbations distinctes sont requises ({distincts}/2 reçue(s))."),

            _ => new VerdictDeCircuit(false, "Type de circuit non reconnu.")
        };
    }
}
