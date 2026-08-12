using N4Sentinel.Domain.Common;

namespace N4Sentinel.Domain.Supervision;

/// <summary>Ce qu'un signal collecté dit du composant.</summary>
public enum VerdictDeSignal
{
    /// <summary>Le signal va dans le sens d'un composant en état de fonctionner.</summary>
    Favorable,

    /// <summary>Le signal est lu, et il est mauvais.</summary>
    Defavorable,

    /// <summary>Le signal est lu, correct, mais hors des seuils attendus.</summary>
    Degrade,

    /// <summary>Le signal n'a pas pu être collecté. Ce n'est pas une bonne nouvelle : c'est une absence.</summary>
    Indisponible,

    /// <summary>Le signal date trop pour être opposé à l'état courant.</summary>
    Perime
}

/// <summary>Un signal tel qu'il entre dans la consolidation.</summary>
/// <param name="Type">Nature du signal : service Windows, port TCP, endpoint HTTP, Cluster Service…</param>
/// <param name="Verdict">Ce que le signal indique.</param>
/// <param name="Detail">Ce qui a réellement été lu, pour que l'état affiché soit justifiable.</param>
/// <param name="SuffitSeulAConclure">
/// Faux pour un signal qui, isolé, ne prouve rien — l'état d'un service Windows notamment.
/// </param>
/// <param name="Transition">
/// Transition constatée par le connecteur. C'est lui qui la voit — un service « StartPending »
/// n'est ni haut ni bas —, la consolidation ne peut pas la deviner après coup.
/// </param>
public sealed record SignalConsolidable(
    string Type,
    VerdictDeSignal Verdict,
    string? Detail = null,
    bool SuffitSeulAConclure = true,
    TransitionObservee Transition = TransitionObservee.Aucune);

/// <summary>État consolidé et sa justification.</summary>
/// <param name="Etat">L'état retenu.</param>
/// <param name="Justification">Pourquoi cet état, en une phrase opposable.</param>
/// <param name="SignauxManquants">Signaux attendus et non obtenus, jamais passés sous silence.</param>
public sealed record EtatConsolide(
    ComponentHealth Etat,
    string Justification,
    IReadOnlyList<string> SignauxManquants);

/// <summary>
/// FR-016 — établissement de l'état réel à partir de plusieurs signaux croisés.
///
/// Trois règles gouvernent tout le reste, et elles viennent du cahier des charges :
///  1. un service déclaré Running ne suffit pas à conclure ;
///  2. des signaux contradictoires donnent « À confirmer », jamais une moyenne ;
///  3. l'absence d'un signal n'est jamais interprétée comme une absence d'anomalie.
///
/// La troisième est la plus facile à trahir sans le vouloir : il est tentant de traiter un
/// contrôle qui n'a pas répondu comme un contrôle qui n'a rien signalé. C'est l'inverse.
/// </summary>
public static class ConsolidationDEtat
{
    public static EtatConsolide Consolider(IReadOnlyCollection<SignalConsolidable> signaux)
    {
        ArgumentNullException.ThrowIfNull(signaux);

        var manquants = signaux
            .Where(s => s.Verdict is VerdictDeSignal.Indisponible or VerdictDeSignal.Perime)
            .Select(s => $"{s.Type} ({Libelle(s.Verdict)})")
            .ToList();

        var exploitables = signaux
            .Where(s => s.Verdict is VerdictDeSignal.Favorable
                        or VerdictDeSignal.Defavorable
                        or VerdictDeSignal.Degrade)
            .ToList();

        if (exploitables.Count == 0)
        {
            return new EtatConsolide(
                ComponentHealth.Inconnu,
                signaux.Count == 0
                    ? "Aucun signal collecté."
                    : "Aucun signal exploitable : tous sont indisponibles ou périmés.",
                manquants);
        }

        var defavorables = exploitables.Where(s => s.Verdict == VerdictDeSignal.Defavorable).ToList();
        var favorables = exploitables.Where(s => s.Verdict == VerdictDeSignal.Favorable).ToList();
        var degrades = exploitables.Where(s => s.Verdict == VerdictDeSignal.Degrade).ToList();

        // Contradiction : on ne tranche pas à la majorité, on le dit.
        if (defavorables.Count > 0 && (favorables.Count > 0 || degrades.Count > 0))
        {
            return new EtatConsolide(
                ComponentHealth.AConfirmer,
                "Signaux contradictoires : "
                + $"{Enumerer(defavorables)} défavorable(s) contre {Enumerer([.. favorables, .. degrades])}.",
                manquants);
        }

        if (defavorables.Count > 0)
        {
            return new EtatConsolide(
                ComponentHealth.Arrete,
                $"Tous les signaux exploitables sont défavorables : {Enumerer(defavorables)}.",
                manquants);
        }

        if (degrades.Count > 0)
        {
            return new EtatConsolide(
                ComponentHealth.Degrade,
                $"Signaux hors seuils : {Enumerer(degrades)}.",
                manquants);
        }

        // À partir d'ici, tous les signaux exploitables sont favorables.

        if (manquants.Count > 0)
        {
            return new EtatConsolide(
                ComponentHealth.AConfirmer,
                "Signaux favorables mais incomplets : "
                + $"{string.Join(", ", manquants)} n'ont pas pu être pris en compte.",
                manquants);
        }

        if (favorables.All(s => !s.SuffitSeulAConclure))
        {
            return new EtatConsolide(
                ComponentHealth.AConfirmer,
                $"Seuls des signaux non concluants isolément sont favorables : {Enumerer(favorables)}. "
                + "Un service déclaré démarré ne prouve pas qu'un composant est opérationnel.",
                manquants);
        }

        return new EtatConsolide(
            ComponentHealth.Operationnel,
            $"Signaux concordants et complets : {Enumerer(favorables)}.",
            manquants);
    }

    private static string Enumerer(IReadOnlyCollection<SignalConsolidable> signaux) =>
        string.Join(", ", signaux.Select(s => s.Type));

    private static string Libelle(VerdictDeSignal verdict) => verdict switch
    {
        VerdictDeSignal.Indisponible => "indisponible",
        VerdictDeSignal.Perime => "périmé",
        VerdictDeSignal.Favorable => "favorable",
        VerdictDeSignal.Defavorable => "défavorable",
        VerdictDeSignal.Degrade => "dégradé",
        _ => verdict.ToString()
    };
}
