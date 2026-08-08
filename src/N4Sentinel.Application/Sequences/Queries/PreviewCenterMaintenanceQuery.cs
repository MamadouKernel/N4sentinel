using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Services;

namespace N4Sentinel.Application.Sequences.Queries;

public sealed record CenterMaintenanceStepDto(
    int Position,
    string Label,
    Guid? ComponentId,
    string? ComponentName,
    WorkflowStepAction Action,
    string SuccessCriteria,
    bool IsVerification,
    string? SourceReference);

public sealed record CenterMaintenancePlanDto(
    Guid EnvironmentId,
    string EnvironmentName,
    CenterRoleStrategy Strategy,
    string Summary,
    IReadOnlyList<CenterMaintenanceStepDto> Steps,
    IReadOnlyList<string> Warnings);

/// <summary>
/// FR-046 / FR-047 : déroulé d'une intervention sur le couple Center / Standby, selon que le rôle actif doit
/// rester sur le primaire ou qu'une bascule est acceptée. Prévisualisation seule.
/// </summary>
public sealed record PreviewCenterMaintenanceQuery(Guid EnvironmentId, CenterRoleStrategy Strategy)
    : IRequest<CenterMaintenancePlanDto?>;

public sealed class PreviewCenterMaintenanceQueryHandler(
    IEnvironmentRepository environments,
    IComponentRepository components) : IRequestHandler<PreviewCenterMaintenanceQuery, CenterMaintenancePlanDto?>
{
    public async Task<CenterMaintenancePlanDto?> Handle(
        PreviewCenterMaintenanceQuery request, CancellationToken cancellationToken)
    {
        var environment = await environments.GetByIdAsync(request.EnvironmentId, cancellationToken);

        if (environment is null)
        {
            return null;
        }

        var environmentComponents = await components.ListByEnvironmentAsync(request.EnvironmentId, cancellationToken);
        var plan = CenterRoleMaintenancePlanner.Plan(environmentComponents, request.Strategy);

        return new CenterMaintenancePlanDto(
            environment.Id,
            environment.Name,
            plan.Strategy,
            plan.Summary,
            plan.Steps
                .Select(s => new CenterMaintenanceStepDto(
                    s.Position, s.Label, s.ComponentId, s.ComponentName, s.Action, s.SuccessCriteria,
                    s.IsVerification, s.SourceReference))
                .ToList(),
            plan.Warnings);
    }
}
