using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Application.Orchestration;

namespace N4Sentinel.Data.Orchestration;

/// <summary>
/// Persistance de l'état du moteur. Le contrat est déclaré par l'orchestrateur ; cette classe
/// se contente de le servir.
/// </summary>
public sealed class EtatDExecutionPersiste(ApplicationDbContext contexte, IClock horloge)
    : IEtatDExecutionPersiste
{
    private static readonly ExecutionStatus[] EtatsNonTermines =
    [
        ExecutionStatus.EnPreparation,
        ExecutionStatus.EnAttenteDApprobation,
        ExecutionStatus.EnCours,
        ExecutionStatus.EnPause,
        ExecutionStatus.AnnulationDemandee,
        ExecutionStatus.ReconciliationRequise
    ];

    public Task<OperationExecution?> LireAsync(Guid executionId, CancellationToken cancellationToken) =>
        contexte.Executions
            .Include(e => e.Etapes)
            .FirstOrDefaultAsync(e => e.Id == executionId, cancellationToken);

    public async Task<IReadOnlyList<OperationExecution>> LireLesExecutionsNonTermineesAsync(
        CancellationToken cancellationToken) =>
        await contexte.Executions
            .Include(e => e.Etapes)
            .Where(e => EtatsNonTermines.Contains(e.Statut))
            .ToListAsync(cancellationToken);

    public Task<VerrouDOperation?> LireLeVerrouActifAsync(
        Guid environnementId,
        CancellationToken cancellationToken)
    {
        var maintenant = horloge.MaintenantUtc;

        return contexte.VerrousDOperation
            .FirstOrDefaultAsync(
                v => v.EnvironmentId == environnementId
                     && v.LibereLe == null
                     && v.ExpireLe > maintenant,
                cancellationToken);
    }

    public async Task PoserLeVerrouAsync(VerrouDOperation verrou, CancellationToken cancellationToken)
    {
        contexte.VerrousDOperation.Add(verrou);
        await contexte.SaveChangesAsync(cancellationToken);
    }

    public async Task LibererLeVerrouAsync(Guid executionId, string par, CancellationToken cancellationToken)
    {
        var verrous = await contexte.VerrousDOperation
            .Where(v => v.ExecutionId == executionId && v.LibereLe == null)
            .ToListAsync(cancellationToken);

        foreach (var verrou in verrous)
        {
            verrou.LibereLe = horloge.MaintenantUtc;
            verrou.LiberePar = par;
        }

        await contexte.SaveChangesAsync(cancellationToken);
    }

    public Task EnregistrerAsync(CancellationToken cancellationToken) =>
        contexte.SaveChangesAsync(cancellationToken);

    public async Task<IReadOnlyList<WorkflowStepDefinition>> LireLesDefinitionsDEtapesAsync(
        Guid workflowVersionId, CancellationToken cancellationToken) =>
        await contexte.Set<WorkflowStepDefinition>()
            .AsNoTracking()
            .Where(d => d.WorkflowVersionId == workflowVersionId)
            .ToListAsync(cancellationToken);

    public async Task<WorkflowType?> LireLeTypeDuWorkflowAsync(
        Guid workflowVersionId, CancellationToken cancellationToken)
    {
        var types = await contexte.VersionsDeWorkflow
            .AsNoTracking()
            .Where(v => v.Id == workflowVersionId)
            .Join(contexte.Workflows, v => v.WorkflowId, w => w.Id, (_, w) => w.Type)
            .ToListAsync(cancellationToken);

        return types.Count == 0 ? null : types[0];
    }

    public Task<N4Component?> LireLeComposantAsync(Guid composantId, CancellationToken cancellationToken) =>
        contexte.Composants.AsNoTracking().FirstOrDefaultAsync(c => c.Id == composantId, cancellationToken);

    public async Task<IReadOnlyList<Guid>> LireLesIdentifiantsDesExecutionsEnCoursAsync(
        CancellationToken cancellationToken) =>
        await contexte.Executions
            .AsNoTracking()
            .Where(e => e.Statut == ExecutionStatus.EnCours)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);
}
