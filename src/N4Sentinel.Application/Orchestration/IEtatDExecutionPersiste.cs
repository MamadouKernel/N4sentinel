using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Orchestration;

/// <summary>
/// Accès persistant à l'état du moteur.
///
/// Le contrat vit dans la couche partagée et non dans l'orchestrateur : la couche Données
/// l'implémente, et les deux couches techniques ne doivent pas se connaître — c'est une règle
/// du §3.15, vérifiée par les tests d'architecture.
/// </summary>
public interface IEtatDExecutionPersiste
{
    Task<OperationExecution?> LireAsync(Guid executionId, CancellationToken cancellationToken);

    /// <summary>Exécutions qui ne sont pas dans un état terminal — celles qu'un redémarrage doit reprendre.</summary>
    Task<IReadOnlyList<OperationExecution>> LireLesExecutionsNonTermineesAsync(
        CancellationToken cancellationToken);

    Task<VerrouDOperation?> LireLeVerrouActifAsync(Guid environnementId, CancellationToken cancellationToken);

    Task PoserLeVerrouAsync(VerrouDOperation verrou, CancellationToken cancellationToken);

    Task LibererLeVerrouAsync(Guid executionId, string par, CancellationToken cancellationToken);

    Task EnregistrerAsync(CancellationToken cancellationToken);

    // — Sprint 7 : ce qu'il faut pour exécuter réellement une étape —

    /// <summary>Définitions des étapes d'une version de workflow — action, cible, seuils, garde-fous.</summary>
    Task<IReadOnlyList<WorkflowStepDefinition>> LireLesDefinitionsDEtapesAsync(
        Guid workflowVersionId, CancellationToken cancellationToken);

    Task<N4Component?> LireLeComposantAsync(Guid composantId, CancellationToken cancellationToken);

    /// <summary>Identifiants des exécutions actuellement « En cours » — ce que le fond de tâche fait avancer.</summary>
    Task<IReadOnlyList<Guid>> LireLesIdentifiantsDesExecutionsEnCoursAsync(CancellationToken cancellationToken);
}
