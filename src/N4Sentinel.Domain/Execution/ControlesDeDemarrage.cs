using N4Sentinel.Domain.Common;

namespace N4Sentinel.Domain.Execution;

/// <summary>État d'un composant tel que la supervision l'a relu, au moment de décider.</summary>
/// <param name="Nom">Désignation du composant, telle qu'elle sera affichée à l'opérateur.</param>
/// <param name="RoleActif">
/// Pour un Center : détient-il réellement le rôle actif ? Un service démarré ne l'implique pas,
/// et c'est toute la difficulté du couple Center/Standby.
/// </param>
public sealed record EtatConstateDUnComposant(
    string Nom,
    N4ComponentKind Kind,
    ComponentHealth Sante,
    bool RoleActif = false);

/// <summary>Verdict d'un contrôle préalable au démarrage.</summary>
/// <param name="Autorise">Faux si le démarrage ne doit pas commencer.</param>
/// <param name="Motif">Formulation opposable, destinée à l'écran et au journal.</param>
/// <param name="ComposantsEnCause">Ce qu'il faut traiter avant de recommencer, dans l'ordre.</param>
public sealed record VerdictDeDemarrage(
    bool Autorise,
    string Motif,
    IReadOnlyList<string> ComposantsEnCause);

/// <summary>
/// Sprint 8 — les verrous qui empêchent les erreurs de séquence les plus coûteuses au
/// redémarrage d'un écosystème N4.
///
/// Toutes ces règles partagent un principe : elles refusent sur un **état relu**, jamais sur un
/// état supposé. Un démarrage lancé sur une hypothèse fausse ne se rattrape pas — il faut tout
/// arrêter et recommencer, en pleine fenêtre d'exploitation.
/// </summary>
public static class ControlesDeDemarrage
{
    /// <summary>
    /// « Démarrage complet impossible si un composant reste actif : la solution le liste et
    /// propose son arrêt dans le bon ordre » (plan de sprints, S8).
    ///
    /// Démarrer par-dessus un écosystème à moitié debout produit les incidents les plus
    /// difficiles à diagnostiquer : deux instances du même rôle, des files consommées deux fois,
    /// un cluster qui refuse d'admettre un nœud déjà membre. Le refus n'est donc pas une
    /// précaution, c'est la seule issue sûre.
    ///
    /// L'ordre d'arrêt proposé est celui de l'éditeur, pris à la même table que partout ailleurs.
    /// </summary>
    public static VerdictDeDemarrage VerifierQueToutEstArrete(
        IReadOnlyList<EtatConstateDUnComposant> composants)
    {
        ArgumentNullException.ThrowIfNull(composants);

        var debout = composants
            .Where(c => c.Sante is ComponentHealth.Operationnel or ComponentHealth.Degrade)
            .ToList();

        if (debout.Count == 0)
        {
            return new VerdictDeDemarrage(true, "Aucun composant n'est resté actif.", []);
        }

        // Trié dans l'ordre d'arrêt : la liste n'est pas un constat, c'est un plan d'action.
        var aArreter = debout
            .OrderBy(c => SequenceDArretDeReferenceN4.RangDe(c.Kind) ?? int.MaxValue)
            .ThenBy(c => c.Nom, StringComparer.Ordinal)
            .Select(c => c.Nom)
            .ToList();

        return new VerdictDeDemarrage(
            false,
            $"{debout.Count} composant(s) sont encore actifs. Un démarrage complet ne peut pas "
            + "commencer par-dessus : arrêtez-les d'abord, dans l'ordre indiqué.",
            aArreter);
    }

    /// <summary>
    /// « XPS bloqué tant que le Bridge n'est pas confirmé opérationnel » (plan de sprints, S8).
    ///
    /// XPS parle à N4 par le Bridge. Démarré avant lui, il ne trouve pas son interlocuteur,
    /// échoue à s'initialiser, et laisse un état intermédiaire dont on ne sort qu'en le
    /// redémarrant — après avoir compris pourquoi, ce qui prend le plus de temps.
    ///
    /// « Confirmé opérationnel » exclut délibérément l'état dégradé : un Bridge qui répond mal
    /// n'est pas un Bridge sur lequel on démarre XPS.
    /// </summary>
    public static VerdictDeDemarrage VerifierLePrerequisDeXps(
        EtatConstateDUnComposant? bridge)
    {
        if (bridge is null)
        {
            return new VerdictDeDemarrage(
                false,
                "Aucun Bridge daemon n'est connu du référentiel : le prérequis de XPS ne peut "
                + "pas être vérifié, donc pas être tenu pour satisfait.",
                []);
        }

        if (bridge.Sante == ComponentHealth.Operationnel)
        {
            return new VerdictDeDemarrage(true, $"{bridge.Nom} est opérationnel.", []);
        }

        return new VerdictDeDemarrage(
            false,
            $"XPS ne peut pas démarrer : {bridge.Nom} est « {bridge.Sante} » et non opérationnel. "
            + "XPS communique avec N4 par le Bridge ; démarré avant lui, il échoue à s'initialiser.",
            [bridge.Nom]);
    }

    /// <summary>
    /// « Détection du conflit où deux Center seraient actifs » (plan de sprints, S8 ; §5.7 des
    /// procédures d'exploitation).
    ///
    /// C'est l'incident que la distinction Center/Standby existe pour éviter, et il ne se voit
    /// pas dans l'état des services : les deux sont « démarrés ». Seul le rôle actif, relu côté
    /// applicatif, permet de le constater — raison pour laquelle il est porté par le composant
    /// et non déduit de son service.
    /// </summary>
    public static VerdictDeDemarrage DetecterUnConflitDeCenter(
        IReadOnlyList<EtatConstateDUnComposant> composants)
    {
        ArgumentNullException.ThrowIfNull(composants);

        var actifs = composants
            .Where(c => c.Kind is N4ComponentKind.CenterNode or N4ComponentKind.StandbyCenterNode)
            .Where(c => c.RoleActif)
            .OrderBy(c => c.Nom, StringComparer.Ordinal)
            .ToList();

        if (actifs.Count <= 1)
        {
            return new VerdictDeDemarrage(
                true,
                actifs.Count == 1
                    ? $"{actifs[0].Nom} détient seul le rôle actif."
                    : "Aucune instance ne détient le rôle actif.",
                []);
        }

        return new VerdictDeDemarrage(
            false,
            $"{actifs.Count} instances détiennent simultanément le rôle actif. C'est un incident "
            + "en soi : il faut décider laquelle conserver et arrêter les autres avant de "
            + "poursuivre. Aucune ne doit être arrêtée au hasard.",
            [.. actifs.Select(c => c.Nom)]);
    }

    /// <summary>
    /// « Cluster Nodes un par un, chacun ACTIVE et pleinement initialisé avant le suivant »
    /// (plan de sprints, S8).
    ///
    /// Un nœud encore en cours d'initialisation ne bloque pas le démarrage du suivant : il fait
    /// attendre. La nuance est réelle — bloquer signalerait une erreur, alors qu'il n'y en a
    /// pas, et pousserait un opérateur à forcer ce qu'il suffisait d'attendre.
    /// </summary>
    public static VerdictDeDemarrage VerifierLeNoeudPrecedent(EtatConstateDUnComposant? precedent)
    {
        if (precedent is null)
        {
            return new VerdictDeDemarrage(true, "Premier nœud de la séquence.", []);
        }

        return precedent.Sante switch
        {
            ComponentHealth.Operationnel => new VerdictDeDemarrage(
                true, $"{precedent.Nom} est pleinement initialisé.", []),

            ComponentHealth.Degrade or ComponentHealth.AConfirmer or ComponentHealth.Inconnu =>
                new VerdictDeDemarrage(
                    false,
                    $"{precedent.Nom} n'a pas confirmé son initialisation complète "
                    + $"(état « {precedent.Sante} »). Le nœud suivant attend : les nœuds "
                    + "rejoignent le cluster un par un, jamais en parallèle.",
                    [precedent.Nom]),

            _ => new VerdictDeDemarrage(
                false,
                $"{precedent.Nom} n'est pas démarré. La séquence ne peut pas se poursuivre "
                + "en laissant un nœud derrière elle.",
                [precedent.Nom])
        };
    }
}
