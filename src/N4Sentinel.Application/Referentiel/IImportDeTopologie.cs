using N4Sentinel.Domain.Referentiel;

namespace N4Sentinel.Application.Referentiel;

/// <summary>Ce que l'import a fait, et ce qu'il n'a délibérément pas fait.</summary>
/// <param name="ComposantsCrees">Composants absents du référentiel, ajoutés en brouillon.</param>
/// <param name="ComposantsMisAJour">Composants déjà présents, dont la fiche technique a changé.</param>
/// <param name="ComposantsInchanges">Composants déjà conformes à la topologie.</param>
/// <param name="Anomalies">Ce que le fichier ne permettait pas de reprendre, motif à l'appui.</param>
public sealed record RapportDImport(
    int ComposantsCrees,
    int ComposantsMisAJour,
    int ComposantsInchanges,
    IReadOnlyList<string> WorkflowsGeneres,
    IReadOnlyList<string> Anomalies,
    bool Applique,
    string Motif);

/// <summary>
/// Reprend une topologie décrite par la configuration des scripts d'exploitation (SOP-2) dans
/// le référentiel, et en dérive les séquences d'arrêt et de démarrage.
///
/// Trois garanties, qui expliquent pourquoi c'est un import et non une synchronisation :
///
/// **Rien n'est activé.** Composants et versions de workflow sont créés en brouillon. Le cycle
/// de validation du Sprint 2 s'applique tel quel : un fichier de configuration n'a pas autorité
/// pour rendre un scénario exécutable, seul un validateur habilité l'a.
///
/// **Rien n'est supprimé.** Un composant du référentiel absent du fichier n'est pas retiré. Le
/// fichier peut décrire un périmètre partiel, et retirer un composant est une décision tracée,
/// pas l'effet de bord d'un import.
///
/// **L'opération est rejouable.** Réimporter une topologie inchangée ne crée rien et ne modifie
/// rien : c'est ce qui permet de corriger le fichier puis de relancer sans redouter les doublons.
/// </summary>
public interface IImportDeTopologie
{
    Task<RapportDImport> ImporterAsync(
        Guid environnementId,
        ConfigurationN4 configuration,
        bool genererLesSequences,
        CancellationToken cancellationToken = default);
}
