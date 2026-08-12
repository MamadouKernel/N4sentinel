namespace N4Sentinel.Domain.Execution;

/// <summary>Verdict sur l'ouverture d'un arrêt forcé pour une étape qui ne se conclut pas.</summary>
/// <param name="ArretForceOuvert">Vrai si le forçage peut être proposé à un acteur habilité.</param>
/// <param name="DelaiRestant">Ce qu'il reste à attendre avant de pouvoir le proposer.</param>
/// <param name="Motif">Formulation opposable, destinée à l'écran et au journal.</param>
public sealed record VerdictDEscalade(bool ArretForceOuvert, TimeSpan DelaiRestant, string Motif);

/// <summary>
/// Sprint 7 — « arrêt forcé proposé seulement après délai, confirmation et contrôle
/// d'autorisation, jamais automatique » (plan de sprints, S7).
///
/// Un service N4 bloqué en <c>Stopping</c> finit très souvent par s'arrêter seul : forcer trop
/// tôt, c'est tuer un processus qui était en train de vider proprement ses files. Le délai
/// déclaré sur l'étape (<c>WorkflowStepDefinition.TimeoutSecondes</c>) est donc un plancher, pas
/// une simple indication — d'où une règle de domaine plutôt qu'un test noyé dans le moteur.
///
/// Cette politique n'ouvre qu'une possibilité. Les deux autres verrous — confirmation explicite
/// et droit <c>ExecuterUneOperationSensible</c> — restent vérifiés par l'appelant, comme partout
/// ailleurs depuis le Sprint 6 : le moteur enregistre une décision autorisée, il ne l'autorise pas.
/// </summary>
public static class PolitiqueDEscalade
{
    public static VerdictDEscalade EvaluerLArretForce(
        DateTimeOffset? debutDeLEtape,
        DateTimeOffset maintenant,
        int timeoutSecondes)
    {
        if (debutDeLEtape is null)
        {
            return new VerdictDEscalade(false, TimeSpan.Zero,
                "Aucune commande engagée sur cette étape : il n'y a rien à forcer.");
        }

        // Un timeout nul ou négatif ne vaut pas « forçage immédiat » : ce serait faire d'une
        // définition de workflow incomplète une autorisation de tuer un processus sans attendre.
        var delai = TimeSpan.FromSeconds(Math.Max(1, timeoutSecondes));
        var ecoule = maintenant - debutDeLEtape.Value;

        if (ecoule < delai)
        {
            var restant = delai - ecoule;
            return new VerdictDEscalade(false, restant,
                $"Arrêt forcé indisponible : la commande n'a pas encore dépassé son délai normal "
                + $"({Arrondir(restant)} s restantes sur {Arrondir(delai)} s).");
        }

        return new VerdictDEscalade(true, TimeSpan.Zero,
            $"La commande dépasse son délai normal de {Arrondir(delai)} s "
            + $"(engagée depuis {Arrondir(ecoule)} s) : un arrêt forcé peut être proposé, "
            + "sous confirmation explicite d'un acteur habilité.");
    }

    /// <summary>Arrondi au supérieur : annoncer « 0 s restantes » alors qu'il en reste induirait en erreur.</summary>
    private static int Arrondir(TimeSpan duree) => (int)Math.Ceiling(duree.TotalSeconds);
}
