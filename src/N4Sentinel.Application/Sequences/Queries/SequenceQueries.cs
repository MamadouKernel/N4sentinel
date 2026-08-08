using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Sequences.Dtos;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Services;

namespace N4Sentinel.Application.Sequences.Queries;

public sealed record ListSequenceTemplatesQuery : IRequest<IReadOnlyList<SequenceTemplateDto>>;

public sealed class ListSequenceTemplatesQueryHandler(ISequenceTemplateRepository templates)
    : IRequestHandler<ListSequenceTemplatesQuery, IReadOnlyList<SequenceTemplateDto>>
{
    public async Task<IReadOnlyList<SequenceTemplateDto>> Handle(
        ListSequenceTemplatesQuery request, CancellationToken cancellationToken)
    {
        var all = await templates.ListAllAsync(cancellationToken);

        return all
            .OrderBy(t => t.WorkflowType)
            .ThenBy(t => t.TemplateKey)
            .ThenByDescending(t => t.VersionNumber)
            .Select(SequencesMapper.ToDto)
            .ToList();
    }
}

public sealed record GetSequenceTemplateByIdQuery(Guid Id) : IRequest<SequenceTemplateDto?>;

public sealed class GetSequenceTemplateByIdQueryHandler(ISequenceTemplateRepository templates)
    : IRequestHandler<GetSequenceTemplateByIdQuery, SequenceTemplateDto?>
{
    public async Task<SequenceTemplateDto?> Handle(
        GetSequenceTemplateByIdQuery request, CancellationToken cancellationToken)
    {
        var template = await templates.GetByIdAsync(request.Id, cancellationToken);

        return template?.ToDto();
    }
}

/// <summary>
/// Déplie la séquence active sur la topologie réelle de l'environnement et retourne le plan prévisionnel,
/// sans rien créer. C'est l'écran de contrôle avant génération : on voit exactement combien d'étapes seront
/// produites pour N Cluster Nodes, et ce qui manque au référentiel.
/// </summary>
public sealed record PreviewSequencePlanQuery(Guid EnvironmentId, WorkflowType WorkflowType)
    : IRequest<SequencePlanDto?>;

public sealed class PreviewSequencePlanQueryHandler(
    ISequenceTemplateRepository templates,
    IEnvironmentRepository environments,
    IComponentRepository components)
    : IRequestHandler<PreviewSequencePlanQuery, SequencePlanDto?>
{
    public async Task<SequencePlanDto?> Handle(PreviewSequencePlanQuery request, CancellationToken cancellationToken)
    {
        var environment = await environments.GetByIdAsync(request.EnvironmentId, cancellationToken);

        if (environment is null)
        {
            return null;
        }

        var template = await templates.GetActiveForEnvironmentAsync(
            request.EnvironmentId, request.WorkflowType, cancellationToken);

        if (template is null)
        {
            return null;
        }

        var environmentComponents = await components.ListByEnvironmentAsync(request.EnvironmentId, cancellationToken);
        var plan = SequencePlanner.Plan(template, environmentComponents);

        return new SequencePlanDto(
            environment.Id,
            environment.Name,
            template.Id,
            template.Name,
            template.WorkflowType,
            plan.Steps.Select(SequencesMapper.ToDto).ToList(),
            plan.Warnings);
    }
}
