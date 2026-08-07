using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Diagnostics.Commands;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Diagnostics;

public class CreateDiagnosticRuleCommandHandlerTests
{
    private readonly IDiagnosticRuleRepository rules = Substitute.For<IDiagnosticRuleRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private CreateDiagnosticRuleCommandHandler CreateHandler() => new(rules, unitOfWork);

    private static CreateDiagnosticRuleCommand ValidCommand(string ruleKey = "RULE-NET-001") => new(
        ruleKey, DiagnosticDomain.Network, "Perte > 5%", "Sondes réseau", "Coupure réseau",
        DiagnosticSeverity.High, "Pondération perte/latence", null, "Escalader réseau");

    [Fact]
    public async Task Handle_DuplicateRuleKey_ThrowsDomainRuleException()
    {
        var existing = new DiagnosticRule(
            "RULE-NET-001", DiagnosticDomain.Network, "cond", "sources", "hyp", DiagnosticSeverity.Low, "method", null, "reco");
        rules.ListByRuleKeyAsync("RULE-NET-001", Arg.Any<CancellationToken>()).Returns([existing]);
        var handler = CreateHandler();

        var act = () => handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainRuleException>();
    }

    [Fact]
    public async Task Handle_NewRuleKey_CreatesDraftVersion1AndSaves()
    {
        rules.ListByRuleKeyAsync("RULE-NET-001", Arg.Any<CancellationToken>()).Returns([]);
        var handler = CreateHandler();

        var id = await handler.Handle(ValidCommand(), CancellationToken.None);

        id.Should().NotBeEmpty();
        rules.Received(1).Add(Arg.Is<DiagnosticRule>(r =>
            r!.RuleKey == "RULE-NET-001" && r.VersionNumber == 1 && r.Status == DiagnosticRuleStatus.Draft));
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
