using System.Text.RegularExpressions;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Assistant.Dtos;

namespace N4Sentinel.Application.Assistant.Queries;

/// <summary>
/// FR-082 (question en langage naturel, réponse synthétique) + FR-083 (chaque réponse cite le document, la
/// section et l'emplacement — ici la ligne concordante) + FR-085 (en l'absence de source suffisante, le
/// signaler et recommander une vérification/escalade).
///
/// Garde-fou FR-084 (l'assistant ne doit jamais déclencher une action technique) appliqué structurellement :
/// ce handler ne dépend que d'<see cref="IDocumentRepository"/> en lecture — aucune dépendance à
/// <see cref="IUnitOfWork"/> ni à aucun autre repository capable de muter l'état applicatif. La réponse est un
/// DTO immuable composé uniquement d'extraits de documents ; elle ne référence, ne contient ni ne peut
/// déclencher aucune commande. Toute action doit être lancée séparément dans le workflow habituel, avec ses
/// propres contrôles et validations — jamais depuis cette requête.
/// </summary>
public sealed record AskAssistantQuery(string Question) : IRequest<AssistantAnswerDto>;

public sealed class AskAssistantQueryHandler(IDocumentRepository documents) : IRequestHandler<AskAssistantQuery, AssistantAnswerDto>
{
    private const int MaxSources = 5;
    private static readonly Regex WordPattern = new(@"[\p{L}\p{Nd}]{3,}", RegexOptions.Compiled);

    public async Task<AssistantAnswerDto> Handle(AskAssistantQuery request, CancellationToken cancellationToken)
    {
        var keywords = WordPattern.Matches(request.Question)
            .Select(m => m.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (keywords.Count == 0)
        {
            return new AssistantAnswerDto(
                request.Question, HasSufficientSource: false, Sources: [],
                InsufficientSourceRecommendation:
                    "La question ne contient pas de terme exploitable. Reformulez avec un symptôme, un composant ou un message d'erreur précis.");
        }

        var indexed = await documents.ListIndexedAsync(cancellationToken);
        var scored = new List<AssistantSourceDto>();

        foreach (var document in indexed)
        {
            var lines = document.Content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < lines.Length; i++)
            {
                var score = keywords.Count(k => lines[i].Contains(k, StringComparison.OrdinalIgnoreCase));
                if (score > 0)
                {
                    scored.Add(new AssistantSourceDto(
                        document.Id, document.DocumentKey, document.VersionNumber, document.Title,
                        document.N4Version, i + 1, lines[i].Trim(), score));
                }
            }
        }

        var topSources = scored.OrderByDescending(s => s.RelevanceScore).Take(MaxSources).ToList();

        return topSources.Count == 0
            ? new AssistantAnswerDto(
                request.Question, HasSufficientSource: false, Sources: [],
                InsufficientSourceRecommendation:
                    "Aucune source suffisante dans le corpus indexé pour répondre avec confiance. Vérification manuelle recommandée, ou escalade vers l'équipe Infrastructure/support Navis (FR-085).")
            : new AssistantAnswerDto(request.Question, HasSufficientSource: true, topSources, InsufficientSourceRecommendation: null);
    }
}
