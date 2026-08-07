using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Sops.Queries;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Sops;

public class SuggestSopsForIncidentQueryHandlerTests
{
    private readonly ISopRepository sops = Substitute.For<ISopRepository>();
    private readonly ISopExecutionRepository executions = Substitute.For<ISopExecutionRepository>();

    private SuggestSopsForIncidentQueryHandler CreateHandler() => new(sops, executions);

    private static Sop CreateActiveSop(string sopKey, string title, string objective)
    {
        var sop = new Sop(sopKey, title, objective, null, "Étape 1", null, null, null, null);
        sop.SubmitForValidation();
        sop.Validate();
        sop.Activate();
        return sop;
    }

    [Fact]
    public async Task Handle_MatchesByKeywordAndComputesRealSuccessRateFromExecutions()
    {
        var sop = CreateActiveSop("SOP-CLUSTER-RESTART", "Redémarrage Cluster Node", "Rétablir un Cluster Node en échec");
        sops.ListActiveAsync(Arg.Any<CancellationToken>()).Returns([sop]);

        var succeeded = new SopExecution(sop.Id, sop.VersionNumber, "operateur1");
        succeeded.Complete(resolvedIssue: true);
        var failed = new SopExecution(sop.Id, sop.VersionNumber, "operateur1");
        failed.Complete(resolvedIssue: false);
        var stillRunning = new SopExecution(sop.Id, sop.VersionNumber, "operateur1");

        executions.ListBySopIdAsync(sop.Id, Arg.Any<CancellationToken>()).Returns([succeeded, failed, stillRunning]);
        var handler = CreateHandler();

        var suggestions = await handler.Handle(new SuggestSopsForIncidentQuery("Cluster Node"), CancellationToken.None);

        suggestions.Should().ContainSingle();
        suggestions[0].CompletedExecutionCount.Should().Be(2);
        suggestions[0].SuccessRate.Should().Be(0.5);
    }

    [Fact]
    public async Task Handle_NoKeywordMatch_ReturnsEmpty()
    {
        var sop = CreateActiveSop("SOP-EDI", "Reprise flux EDI", "Relancer un flux EDI en erreur");
        sops.ListActiveAsync(Arg.Any<CancellationToken>()).Returns([sop]);
        var handler = CreateHandler();

        var suggestions = await handler.Handle(new SuggestSopsForIncidentQuery("ActiveMQ"), CancellationToken.None);

        suggestions.Should().BeEmpty();
    }
}
