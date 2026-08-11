using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Operations.Dtos;

namespace N4Sentinel.Application.Operations.Queries;

/// <summary>Analyse d'impact avant une action unitaire ou partielle (FR-041), en lecture seule.</summary>
public sealed record GetOperationImpactAnalysisQuery(Guid EnvironmentId, Guid WorkflowId, Guid WorkflowVersionId)
    : IRequest<OperationImpactAnalysisDto>;

public sealed class GetOperationImpactAnalysisQueryHandler(
    IWorkflowRepository workflows,
    IComponentRepository components) : IRequestHandler<GetOperationImpactAnalysisQuery, OperationImpactAnalysisDto>
{
    public async Task<OperationImpactAnalysisDto> Handle(
        GetOperationImpactAnalysisQuery request, CancellationToken cancellationToken)
    {
        var workflow = await workflows.GetByIdAsync(request.WorkflowId, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow '{request.WorkflowId}' introuvable.");

        var version = workflow.Versions.FirstOrDefault(v => v.Id == request.WorkflowVersionId)
            ?? throw new KeyNotFoundException($"Version '{request.WorkflowVersionId}' introuvable pour ce workflow.");

        var environmentComponents = await components.ListByEnvironmentAsync(request.EnvironmentId, cancellationToken);

        var targetComponentIds = version.Steps
            .Where(s => s.ComponentId is not null)
            .Select(s => s.ComponentId!.Value)
            .Distinct()
            .ToList();

        var impacts = new List<ComponentImpactDto>();
        foreach (var componentId in targetComponentIds)
        {
            var component = environmentComponents.FirstOrDefault(c => c.Id == componentId);
            if (component is null)
            {
                continue;
            }

            // "Composants dépendants" (FR-041) = les autres composants qui déclarent CELUI-CI comme
            // prérequis (impact en aval), pas l'inverse : c'est le rayon d'effet d'une action sur le composant.
            var dependents = environmentComponents
                .Where(c => c.Id != componentId && c.DependsOnComponentIds.Contains(componentId))
                .Select(c => c.Name)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();

            impacts.Add(new ComponentImpactDto(component.Id, component.Name, component.Criticality, dependents));
        }

        return new OperationImpactAnalysisDto(impacts);
    }
}
