using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Operations;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Sprint 6 (FR-013) — décision individuelle d'un approbateur sur une exécution. Une ligne par
/// approbation, jamais réécrite : le circuit double exige deux acteurs distincts, et compter
/// des approbations suppose de pouvoir les distinguer après coup.
/// </summary>
public class ExecutionApproval : Entity
{
    public Guid ExecutionId { get; set; }

    public required string ApprouvePar { get; set; }

    public DecisionDApprobation Decision { get; set; }

    public DateTimeOffset DecideLe { get; set; } = DateTimeOffset.UtcNow;

    public string? Motif { get; set; }
}
