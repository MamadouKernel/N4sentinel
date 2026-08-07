using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Diagnostics;

public sealed record EngineHypothesisResult(
    DiagnosticDomain Domain,
    Guid? AppliedRuleId,
    string? AppliedRuleKey,
    int? AppliedRuleVersion,
    string CauseDescription,
    DiagnosticConfidenceLevel ConfidenceLevel,
    string? SupportingEvidence,
    string? ContradictingEvidence,
    string? MissingInformation,
    string? RecommendedChecks,
    string? SafeActionsOrEscalation);

public sealed class DiagnosticEngineService(
    IDiagnosticSignalRepository signalRepository,
    IDiagnosticRuleRepository ruleRepository,
    IImportedLogFileRepository logFileRepository)
{
    public async Task<IReadOnlyList<EngineHypothesisResult>> EvaluateAsync(
        DiagnosticCase diagnosticCase, CancellationToken cancellationToken)
    {
        var correlation = diagnosticCase.CorrelationReference;
        var signals = await signalRepository.ListByCorrelationAsync(correlation, cancellationToken);
        var logs = await logFileRepository.ListByCorrelationAsync(correlation, cancellationToken);
        var allRules = await ruleRepository.ListAllAsync(cancellationToken);
        var activeRules = allRules.Where(r => r.Status == DiagnosticRuleStatus.Active).ToList();

        var results = new List<EngineHypothesisResult>();
        var domains = Enum.GetValues<DiagnosticDomain>();

        foreach (var domain in domains)
        {
            var domainSignals = signals.Where(s => s.Domain == domain).ToList();
            var domainRules = activeRules.Where(r => r.Domain == domain).ToList();

            if (domainRules.Count == 0)
            {
                continue;
            }

            foreach (var rule in domainRules)
            {
                var matchingSignals = domainSignals
                    .Where(s => s.CollectionStatus == DiagnosticSignalCollectionStatus.Collected &&
                                (string.IsNullOrWhiteSpace(s.Content) ||
                                 s.Content.Contains(rule.ConditionDescription, StringComparison.OrdinalIgnoreCase) ||
                                 rule.ConditionDescription.Contains(s.Source, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                var matchingLogSignatures = logs
                    .Where(l => l.DetectedSignatures != null &&
                                (l.DetectedSignatures.Contains(domain.ToString(), StringComparison.OrdinalIgnoreCase) ||
                                 rule.RequiredSources.Split(',', StringSplitOptions.TrimEntries)
                                     .Any(src => l.FileName.Contains(src, StringComparison.OrdinalIgnoreCase) ||
                                                 (l.Source != null && l.Source.Contains(src, StringComparison.OrdinalIgnoreCase)))))
                    .ToList();

                // Compute Confidence Level
                DiagnosticConfidenceLevel confidence;
                if (matchingSignals.Count >= 2 && matchingSignals.Any(s => s.Reliability == DiagnosticSignalReliability.High) ||
                    (matchingSignals.Count >= 1 && matchingLogSignatures.Count >= 1))
                {
                    confidence = DiagnosticConfidenceLevel.High;
                }
                else if (matchingSignals.Count >= 1 || matchingLogSignatures.Count >= 1 || domainSignals.Any(s => s.CollectionStatus == DiagnosticSignalCollectionStatus.Collected))
                {
                    confidence = DiagnosticConfidenceLevel.Medium;
                }
                else
                {
                    confidence = DiagnosticConfidenceLevel.Low;
                }

                // Evidence texts
                var supportingList = new List<string>();
                if (matchingSignals.Count > 0)
                {
                    supportingList.Add($"Signaux collectés ({matchingSignals.Count}) : {string.Join(", ", matchingSignals.Select(s => s.Source))}");
                }
                if (matchingLogSignatures.Count > 0)
                {
                    supportingList.Add($"Journaux analysés ({matchingLogSignatures.Count}) : {string.Join(", ", matchingLogSignatures.Select(l => l.FileName))}");
                }
                var supporting = supportingList.Count > 0 ? string.Join(" | ", supportingList) : "Aucune preuve directe issue des signaux.";

                var unavailableSignals = domainSignals.Where(s => s.CollectionStatus == DiagnosticSignalCollectionStatus.Unavailable).ToList();
                var missingInfoList = new List<string>();
                if (unavailableSignals.Count > 0)
                {
                    missingInfoList.Add($"Signaux indisponibles ({unavailableSignals.Count}) : {string.Join(", ", unavailableSignals.Select(s => $"{s.Source} ({s.UnavailableReason})"))}");
                }
                if (domainSignals.Count == 0)
                {
                    missingInfoList.Add($"Aucun signal collecté pour le domaine {domain}. Sources requises par la règle : {rule.RequiredSources}");
                }
                var missing = missingInfoList.Count > 0 ? string.Join(" | ", missingInfoList) : null;

                results.Add(new EngineHypothesisResult(
                    Domain: domain,
                    AppliedRuleId: rule.Id,
                    AppliedRuleKey: rule.RuleKey,
                    AppliedRuleVersion: rule.VersionNumber,
                    CauseDescription: $"{rule.Hypothesis} (Règle {rule.RuleKey} v{rule.VersionNumber})",
                    ConfidenceLevel: confidence,
                    SupportingEvidence: supporting,
                    ContradictingEvidence: null,
                    MissingInformation: missing,
                    RecommendedChecks: rule.AdditionalChecks,
                    SafeActionsOrEscalation: rule.Recommendation));
            }
        }

        return results;
    }
}
