using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Sops.Dtos;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Sops.Queries;

/// <summary>
/// FR-089D (réutilisation contrôlée) : propose des SOP validées (Actives) dont le titre/objectif/prérequis
/// correspondent à un mot-clé (symptôme, composant, message d'erreur), avec date, version et taux de réussite.
///
/// Décision de cadrage : le cahier des charges n'impose aucune méthode particulière de mise en correspondance
/// — un score par mots-clés simple (même approche que <see cref="Assistant.Queries.AskAssistantQuery"/>,
/// Sprint 14) est suffisant et honnête ; pas de recherche de similarité sophistiquée non plus justifiée par la
/// spécification. Le taux de réussite est calculé sur les <see cref="SopExecution"/> réellement terminées pour
/// cette SOP — jamais une estimation inventée.
/// </summary>
public sealed record SuggestSopsForIncidentQuery(string Keyword) : IRequest<IReadOnlyList<SopSuggestionDto>>;

public sealed class SuggestSopsForIncidentQueryHandler(ISopRepository sops, ISopExecutionRepository executions)
    : IRequestHandler<SuggestSopsForIncidentQuery, IReadOnlyList<SopSuggestionDto>>
{
    public async Task<IReadOnlyList<SopSuggestionDto>> Handle(
        SuggestSopsForIncidentQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Keyword))
        {
            return [];
        }

        var active = await sops.ListActiveAsync(cancellationToken);
        var suggestions = new List<SopSuggestionDto>();

        foreach (var sop in active)
        {
            var score = CountMatches(sop.Title, request.Keyword) + CountMatches(sop.Objective, request.Keyword) +
                CountMatches(sop.Prerequisites, request.Keyword);

            if (score == 0)
            {
                continue;
            }

            var executionsForSop = await executions.ListBySopIdAsync(sop.Id, cancellationToken);
            var completed = executionsForSop.Where(e => e.Status == SopExecutionStatus.Completed).ToList();
            var successRate = completed.Count == 0
                ? (double?)null
                : (double)completed.Count(e => e.ResolvedIssue == true) / completed.Count;

            suggestions.Add(new SopSuggestionDto(
                sop.Id, sop.SopKey, sop.VersionNumber, sop.Title, sop.Objective, sop.N4Version, sop.UpdatedAtUtc,
                score, completed.Count, successRate));
        }

        return suggestions.OrderByDescending(s => s.MatchScore).ThenByDescending(s => s.SuccessRate).ToList();
    }

    private static int CountMatches(string? text, string keyword) =>
        string.IsNullOrWhiteSpace(text) ? 0 : text.Contains(keyword, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
}
