namespace N4Sentinel.Domain.Orchestration;

/// <summary>
/// Sprint 7 — le pendant *avant* lancement de <see cref="PolitiqueDeTransition"/>, qui ne
/// couvre que l'*après*. Une étape déclarée « confirmation requise » ou « approbation requise »
/// (Sprint 0, jamais consommé jusqu'ici) ne part jamais d'elle-même : la décision revient à un
/// humain, et à un humain distinct du demandeur pour l'approbation — la séparation est vérifiée
/// ailleurs, par <see cref="Habilitations.SeparationDesResponsabilites"/>, pas ici.
///
/// Simplification assumée : si une étape porte les deux exigences à la fois, l'approbation —
/// le geste le plus fort des deux — couvre aussi la confirmation. <see cref="ExecutionStep"/>
/// ne porte qu'une seule décision par étape (Sprint 0) ; suivre les deux indépendamment
/// demanderait une colonne supplémentaire pour un cas que les workflows réels ne combinent pas.
/// </summary>
public static class PolitiqueDeLancement
{
    public static DecisionDePoursuite Evaluer(
        bool confirmationRequise, bool approbationRequise, bool autorisationObtenue)
    {
        if (!confirmationRequise && !approbationRequise)
        {
            return new DecisionDePoursuite(true, false, "Aucune décision humaine requise avant lancement.");
        }

        if (autorisationObtenue)
        {
            return new DecisionDePoursuite(true, false, "Décision humaine déjà obtenue.");
        }

        return approbationRequise
            ? new DecisionDePoursuite(false, true,
                "Approbation requise avant de lancer cette étape, par un acteur distinct du demandeur.")
            : new DecisionDePoursuite(false, true, "Confirmation requise avant de lancer cette étape.");
    }
}
