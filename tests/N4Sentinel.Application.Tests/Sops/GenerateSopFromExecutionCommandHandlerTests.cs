using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Sops.Commands;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Sops;

public class GenerateSopFromExecutionCommandHandlerTests
{
    private readonly ISopExecutionRepository executions = Substitute.For<ISopExecutionRepository>();
    private readonly ISopRepository sops = Substitute.For<ISopRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private GenerateSopFromExecutionCommandHandler CreateHandler() => new(executions, sops, unitOfWork);

    private static GenerateSopFromExecutionCommand ValidCommand(Guid executionId) => new(
        executionId, "SOP-GENERATED", "Titre généré", "Objectif généré", null, null, null, null, null);

    [Fact]
    public async Task Handle_ExecutionNotCompleted_ThrowsDomainRuleException()
    {
        var execution = new SopExecution(Guid.NewGuid(), 1, "operateur1");
        executions.GetByIdAsync(execution.Id, Arg.Any<CancellationToken>()).Returns(execution);
        var handler = CreateHandler();

        var act = () => handler.Handle(ValidCommand(execution.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainRuleException>();
    }

    [Fact]
    public async Task Handle_ExecutionCompletedButDidNotResolveIssue_ThrowsDomainRuleException()
    {
        var execution = new SopExecution(Guid.NewGuid(), 1, "operateur1");
        execution.Complete(resolvedIssue: false);
        executions.GetByIdAsync(execution.Id, Arg.Any<CancellationToken>()).Returns(execution);
        var handler = CreateHandler();

        var act = () => handler.Handle(ValidCommand(execution.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainRuleException>();
    }

    [Fact]
    public async Task Handle_ExecutionResolvedIssue_GeneratesDraftSopFromConfirmedSteps()
    {
        var execution = new SopExecution(Guid.NewGuid(), 1, "operateur1");
        execution.ConfirmNextStep("Arrêter le service", "operateur1", null, null);
        execution.ConfirmNextStep("Redémarrer le service", "operateur1", null, null);
        execution.Complete(resolvedIssue: true);
        executions.GetByIdAsync(execution.Id, Arg.Any<CancellationToken>()).Returns(execution);
        sops.ListBySopKeyAsync("SOP-GENERATED", Arg.Any<CancellationToken>()).Returns([]);
        var handler = CreateHandler();

        var id = await handler.Handle(ValidCommand(execution.Id), CancellationToken.None);

        id.Should().NotBeEmpty();
        sops.Received(1).Add(Arg.Is<Sop>(s =>
            s!.IsGeneratedFromExecution &&
            s.Steps.SequenceEqual(new[] { "Arrêter le service", "Redémarrer le service" })));
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
