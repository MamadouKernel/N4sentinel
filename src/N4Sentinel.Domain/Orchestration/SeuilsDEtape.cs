namespace N4Sentinel.Domain.Orchestration;

/// <summary>Ce que le workflow décide quand une étape ne se termine pas nettement.</summary>
public enum PolitiqueDeSuite
{
    /// <summary>Poursuivre sans rien demander.</summary>
    Poursuivre,

    /// <summary>Poursuivre, mais seulement après confirmation humaine explicite.</summary>
    PoursuivreApresConfirmation,

    /// <summary>Interdire la poursuite.</summary>
    Interdire
}

/// <summary>
/// FR-004 — seuils, délais et politiques d'une étape.
///
/// La règle dure du cahier des charges est portée ici : « les nouvelles tentatives automatiques
/// doivent être interdites pour les actions critiques ou destructrices, sauf si elles ont été
/// explicitement autorisées dans un workflow validé ». Une action destructrice rejouée
/// automatiquement, c'est une double suppression que personne n'a demandée.
/// </summary>
/// <param name="DureeNormale">Durée indicative, sert à repérer une lenteur.</param>
/// <param name="SeuilDAvertissement">Au-delà, l'étape est signalée sans être interrompue.</param>
/// <param name="Timeout">Au-delà, l'étape est arrêtée et déclarée en échec de délai.</param>
/// <param name="TentativesMax">Nombre total de tentatives, la première comprise.</param>
/// <param name="ReessaiAutomatique">Faux : la nouvelle tentative exige une décision humaine.</param>
/// <param name="DelaiEntreTentatives">Attente avant une nouvelle tentative.</param>
/// <param name="ActionCritiqueOuDestructrice">Vrai pour une action qui altère des données ou la configuration.</param>
/// <param name="ReessaiAutomatiqueExplicitementAutorise">
/// Seule échappatoire à l'interdiction ci-dessus, et elle doit venir d'une version validée du workflow.
/// </param>
/// <param name="EnCasDAvertissement">Politique quand l'étape réussit hors seuils.</param>
/// <param name="EnCasDEtatInconnu">Politique quand le résultat n'est pas vérifiable.</param>
public sealed record SeuilsDEtape(
    TimeSpan DureeNormale,
    TimeSpan SeuilDAvertissement,
    TimeSpan Timeout,
    int TentativesMax = 1,
    bool ReessaiAutomatique = false,
    TimeSpan? DelaiEntreTentatives = null,
    bool ActionCritiqueOuDestructrice = false,
    bool ReessaiAutomatiqueExplicitementAutorise = false,
    PolitiqueDeSuite EnCasDAvertissement = PolitiqueDeSuite.PoursuivreApresConfirmation,
    PolitiqueDeSuite EnCasDEtatInconnu = PolitiqueDeSuite.Interdire)
{
    /// <summary>
    /// FR-004 — une nouvelle tentative automatique n'est possible que si le workflow l'a prévue,
    /// et jamais sur une action critique ou destructrice sans autorisation explicite.
    /// </summary>
    public bool AutoriseUneNouvelleTentativeAutomatique(int tentativesDejaFaites)
    {
        if (!ReessaiAutomatique || tentativesDejaFaites >= TentativesMax)
        {
            return false;
        }

        return !ActionCritiqueOuDestructrice || ReessaiAutomatiqueExplicitementAutorise;
    }

    /// <summary>Vrai si la durée écoulée dépasse le seuil d'avertissement sans atteindre le timeout.</summary>
    public bool EstEnAvertissement(TimeSpan ecoule) =>
        ecoule >= SeuilDAvertissement && ecoule < Timeout;

    public bool EstEnTimeout(TimeSpan ecoule) => ecoule >= Timeout;

    /// <summary>Seuils prudents par défaut : aucune reprise automatique, état inconnu bloquant.</summary>
    public static SeuilsDEtape ParDefaut { get; } = new(
        DureeNormale: TimeSpan.FromSeconds(30),
        SeuilDAvertissement: TimeSpan.FromMinutes(2),
        Timeout: TimeSpan.FromMinutes(10));
}
