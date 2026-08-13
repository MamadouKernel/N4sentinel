namespace N4Sentinel.Domain.Execution;

/// <summary>
/// Sprint 7 — ce qu'un connecteur de commande répond à une demande d'action. Distinct de
/// <see cref="Supervision.VerdictDeSignal"/> (Sprint 3) : ici on a demandé un changement d'état,
/// pas lu un état existant.
/// </summary>
public enum ResultatDeCommande
{
    Reussie,
    Echouee,

    /// <summary>La commande est passée, son effet n'est pas encore constaté — un service qui
    /// reste « StopPending » au-delà du délai normal, notamment.</summary>
    EnCours,

    /// <summary>Aucun connecteur ne prend en charge cette action. Devrait être écarté avant
    /// le lancement de l'étape ; conservé ici par prudence, pas par confiance.</summary>
    NonSupportee
}
