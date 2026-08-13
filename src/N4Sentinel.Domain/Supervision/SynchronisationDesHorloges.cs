namespace N4Sentinel.Domain.Supervision;

/// <summary>Horloge d'un serveur, relue à un instant donné.</summary>
/// <param name="Serveur">Hôte interrogé.</param>
/// <param name="Ecart">
/// Écart signé avec l'horloge de référence. Le signe est conservé : un serveur en avance et un
/// serveur en retard ne se diagnostiquent pas de la même façon.
/// </param>
public sealed record EcartDHorloge(string Serveur, TimeSpan Ecart);

/// <summary>Verdict de synchronisation, et ce qu'il faut regarder s'il est mauvais.</summary>
public sealed record VerdictDHorloges(
    bool Synchronisees,
    string Motif,
    IReadOnlyList<string> ServeursHorsTolerance);

/// <summary>
/// SOP-3 — « Horloges synchronisées entre tous les serveurs, écart &lt; 1 seconde », point de
/// contrôle quotidien.
///
/// Ce n'est pas une exigence de confort. Le document en donne la raison, et elle vise
/// directement ce sur quoi l'application fonde ses décisions : un écart d'horloge « n'est
/// presque jamais la cause directe d'un incident visible — c'est ce qui le rend dangereux : il
/// fausse silencieusement les statuts affichés (un nœud actif peut apparaître DISCONNECTED) ».
/// Il figure au Top 10 des causes de P1 identifiées par Navis/Kaleris.
///
/// Or le moteur d'orchestration décide sur ces statuts : il relit l'état réel avant d'émettre,
/// saute une étape dont la cible est déjà dans l'état visé, et conclut une étape sur l'effet
/// constaté. Des horloges désynchronisées ne dégradent donc pas l'affichage — elles corrompent
/// la donnée d'entrée de chaque décision.
/// </summary>
public static class SynchronisationDesHorloges
{
    /// <summary>Tolérance de SOP-3. Une seconde, pas « environ une seconde ».</summary>
    public static TimeSpan ToleranceParDefaut { get; } = TimeSpan.FromSeconds(1);

    public static VerdictDHorloges Evaluer(
        IReadOnlyList<EcartDHorloge> ecarts,
        TimeSpan? tolerance = null)
    {
        ArgumentNullException.ThrowIfNull(ecarts);

        var seuil = tolerance ?? ToleranceParDefaut;

        if (ecarts.Count == 0)
        {
            // Aucune horloge relue ne vaut pas « horloges synchronisées » : c'est précisément
            // le genre de silence qu'un contrôle quotidien existe pour ne pas laisser passer.
            return new VerdictDHorloges(
                false,
                "Aucune horloge n'a pu être relue : la synchronisation ne peut pas être "
                + "confirmée, donc pas être tenue pour acquise.",
                []);
        }

        var horsTolerance = ecarts
            .Where(e => e.Ecart.Duration() >= seuil)
            .OrderByDescending(e => e.Ecart.Duration())
            .ToList();

        if (horsTolerance.Count == 0)
        {
            return new VerdictDHorloges(
                true,
                $"Les {ecarts.Count} serveurs relus sont synchronisés à moins de "
                + $"{Arrondir(seuil)} s.",
                []);
        }

        var pire = horsTolerance[0];

        return new VerdictDHorloges(
            false,
            $"{horsTolerance.Count} serveur(s) hors tolérance, au pire {pire.Serveur} à "
            + $"{Arrondir(pire.Ecart.Duration())} s. Un écart d'horloge fausse silencieusement "
            + "les statuts relus : un nœud actif peut apparaître DISCONNECTED, et une décision "
            + "prise sur cet état porterait sur une réalité qui n'existe pas.",
            [.. horsTolerance.Select(e => e.Serveur)]);
    }

    private static double Arrondir(TimeSpan duree) => Math.Round(duree.TotalSeconds, 2);
}
