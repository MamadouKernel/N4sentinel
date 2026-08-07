using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Diagnostics.Commands;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Diagnostics;

public class ActivateDiagnosticRuleCommandHandlerTests
{
    private readonly IDiagnosticRuleRepository rules = Substitute.For<IDiagnosticRuleRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private ActivateDiagnosticRuleCommandHandler CreateHandler() => new(rules, unitOfWork);

    [Fact]
    public async Task Handle_ActivatingNewVersion_DisablesPreviousActiveSibling()
    {
        var oldVersion = new DiagnosticRule(
            "RULE-NET-001", DiagnosticDomain.Network, "cond", "sources", "hyp", DiagnosticSeverity.Low, "method", null, "reco");
        oldVersion.SubmitForValidation();
        oldVersion.Validate();
        oldVersion.Activate();

        var newVersion = oldVersion.CreateNewVersion();
        newVersion.SubmitForValidation();
        newVersion.Validate();

        rules.GetByIdAsync(newVersion.Id, Arg.Any<CancellationToken>()).Returns(newVersion);
        rules.ListByRuleKeyAsync("RULE-NET-001", Arg.Any<CancellationToken>()).Returns([oldVersion, newVersion]);
        var handler = CreateHandler();

        await handler.Handle(new ActivateDiagnosticRuleCommand(newVersion.Id), CancellationToken.None);

        newVersion.Status.Should().Be(DiagnosticRuleStatus.Active);
        oldVersion.Status.Should().Be(DiagnosticRuleStatus.Disabled);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownRule_ThrowsKeyNotFoundException()
    {
        rules.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DiagnosticRule?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(new ActivateDiagnosticRuleCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
