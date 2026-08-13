namespace N4Sentinel.Domain.Supervision;

/// <summary>Motif d'alerte, tel que FR-054 les énumère.</summary>
public enum MotifDAlerte
{
    /// <summary>Un contrôle n'a pas répondu dans le délai imparti.</summary>
    Timeout,

    /// <summary>Un contrôle a répondu, et la réponse est mauvaise.</summary>
    Echec,

    /// <summary>Des signaux se contredisent : l'état ne peut pas être établi.</summary>
    IncoherenceDEtat,

    /// <summary>Une file de messages croît d'un relevé à l'autre.</summary>
    FileQuiAugmente,

    /// <summary>Le dernier heartbeat est trop ancien pour attester quoi que ce soit.</summary>
    HeartbeatAncien,

    /// <summary>Une ressource — disque, mémoire, sessions — a franchi son seuil critique.</summary>
    RessourceCritique,

    /// <summary>Aucune donnée récente : la supervision elle-même ne voit plus le composant.</summary>
    DonneeTropAncienne
}

/// <summary>Alerte levée sur un composant.</summary>
/// <param name="Motif">Ce qui l'a déclenchée.</param>
/// <param name="Message">Formulation destinée à l'exploitation.</param>
/// <param name="Critique">Vrai quand l'alerte porte sur un composant critique ou un état bas.</param>
public sealed record Alerte(MotifDAlerte Motif, string Message, bool Critique);

/// <summary>Ce qu'il faut savoir d'un composant pour juger de ses alertes.</summary>
/// <param name="Nom">Nom du composant, pour que l'alerte soit lisible sans le rechercher.</param>
/// <param name="Etat">État de supervision consolidé.</param>
/// <param name="Signaux">Signaux du dernier relevé.</param>
/// <param name="DerniereDonnee">Date du dernier relevé.</param>
/// <param name="ValeurDeFilePrecedente">Taille de file au relevé précédent, si connue.</param>
/// <param name="ValeurDeFileCourante">Taille de file au relevé courant, si connue.</param>
/// <param name="Critique">Le composant est-il déclaré critique au référentiel.</param>
public sealed record ContexteDAlerte(
    string Nom,
    EtatDeSupervision Etat,
    IReadOnlyCollection<SignalConsolidable> Signaux,
    DateTimeOffset? DerniereDonnee,
    long? ValeurDeFilePrecedente = null,
    long? ValeurDeFileCourante = null,
    bool Critique = false);

/// <summary>
/// FR-054 — « Une alerte doit être créée lors d'un timeout, d'un échec, d'une incohérence
/// d'état, d'une file qui augmente, d'un heartbeat ancien ou d'une ressource critique. »
///
/// Les règles vivent dans le domaine et non dans le tableau de bord : une alerte qui
/// n'existerait qu'à l'affichage disparaîtrait dès qu'on regarde ailleurs.
///
/// Aucune alerte n'est levée en maintenance ou sur un composant non supervisé : ce sont les
/// deux cas où l'anomalie est attendue, et où alerter reviendrait à apprendre aux exploitants
/// à ignorer les alertes.
/// </summary>
public static class DetecteurDAlertes
{
    /// <summary>Au-delà, la donnée ne dit plus rien de l'état courant.</summary>
    public static readonly TimeSpan AgeMaximalDUneDonnee = TimeSpan.FromMinutes(5);

    /// <summary>Un heartbeat plus ancien que cela n'atteste plus la présence du nœud.</summary>
    public static readonly TimeSpan AgeMaximalDUnHeartbeat = TimeSpan.FromMinutes(2);

    public static IReadOnlyList<Alerte> Detecter(ContexteDAlerte contexte, DateTimeOffset maintenant)
    {
        ArgumentNullException.ThrowIfNull(contexte);

        if (contexte.Etat is EtatDeSupervision.Maintenance or EtatDeSupervision.NonSupervise)
        {
            return [];
        }

        var alertes = new List<Alerte>();

        foreach (var signal in contexte.Signaux)
        {
            if (signal.Verdict == VerdictDeSignal.Defavorable)
            {
                alertes.Add(new Alerte(
                    MotifDAlerte.Echec,
                    $"{contexte.Nom} — {signal.Type} : {signal.Detail}",
                    contexte.Critique));
            }
            else if (signal.Verdict == VerdictDeSignal.Indisponible
                     && EstUnTimeout(signal.Detail))
            {
                alertes.Add(new Alerte(
                    MotifDAlerte.Timeout,
                    $"{contexte.Nom} — {signal.Type} n'a pas répondu dans le délai imparti.",
                    contexte.Critique));
            }
            else if (signal.Verdict == VerdictDeSignal.Degrade)
            {
                alertes.Add(new Alerte(
                    MotifDAlerte.RessourceCritique,
                    $"{contexte.Nom} — {signal.Type} hors seuils : {signal.Detail}",
                    contexte.Critique));
            }
        }

        if (contexte.Etat == EtatDeSupervision.AConfirmer)
        {
            alertes.Add(new Alerte(
                MotifDAlerte.IncoherenceDEtat,
                $"{contexte.Nom} — l'état ne peut pas être établi : signaux contradictoires ou incomplets.",
                contexte.Critique));
        }

        if (contexte.ValeurDeFilePrecedente is { } precedente
            && contexte.ValeurDeFileCourante is { } courante
            && courante > precedente)
        {
            alertes.Add(new Alerte(
                MotifDAlerte.FileQuiAugmente,
                $"{contexte.Nom} — file en croissance : {precedente} → {courante}.",
                contexte.Critique));
        }

        var heartbeat = contexte.Signaux
            .FirstOrDefault(s => s.Type.Contains("Heartbeat", StringComparison.OrdinalIgnoreCase));

        if (heartbeat is not null
            && contexte.DerniereDonnee is { } dateHeartbeat
            && maintenant - dateHeartbeat > AgeMaximalDUnHeartbeat)
        {
            alertes.Add(new Alerte(
                MotifDAlerte.HeartbeatAncien,
                $"{contexte.Nom} — dernier heartbeat il y a "
                + $"{(int)(maintenant - dateHeartbeat).TotalMinutes} min.",
                contexte.Critique));
        }

        // La supervision doit signaler qu'elle ne voit plus, plutôt que d'afficher indéfiniment
        // le dernier état connu comme s'il était courant.
        if (contexte.DerniereDonnee is null)
        {
            alertes.Add(new Alerte(
                MotifDAlerte.DonneeTropAncienne,
                $"{contexte.Nom} — aucun relevé disponible.",
                contexte.Critique));
        }
        else if (maintenant - contexte.DerniereDonnee.Value > AgeMaximalDUneDonnee)
        {
            alertes.Add(new Alerte(
                MotifDAlerte.DonneeTropAncienne,
                $"{contexte.Nom} — dernier relevé il y a "
                + $"{(int)(maintenant - contexte.DerniereDonnee.Value).TotalMinutes} min.",
                contexte.Critique));
        }

        return alertes;
    }

    private static bool EstUnTimeout(string? detail) =>
        detail is not null
        && (detail.Contains("n'a pas répondu", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("Aucune réponse", StringComparison.OrdinalIgnoreCase));
}
