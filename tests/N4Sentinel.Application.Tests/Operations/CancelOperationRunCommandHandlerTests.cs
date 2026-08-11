using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Operations.Commands;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Operations;

public class CancelOperationRunCommandHandlerTests
{
    private readonly IOperationRunRepository operationRuns = Substitute.For<IOperationRunRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private CancelOperationRunCommandHandler CreateHandler() => new(operationRuns, unitOfWork);

    private static OperationRun CreateApprovedRun()
    {
        var steps = new[] { (Guid.NewGuid(), 0, "Démarrer le Bridge", WorkflowStepAction.Start, (Guid?)null, (string?)null) };
        return new OperationRun(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, isProductionEnvironment: false,
            null, null, null, null, "operateur@n4sentinel.local", steps);
    }

    [Fact]
    public async Task Handle_ApprovedRun_CancelsAndSaves()
    {
        var run = CreateApprovedRun();
        operationRuns.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);
        var handler = CreateHandler();

        await handler.Handle(
            new CancelOperationRunCommand(run.Id, "operateur@n4sentinel.local", "Fenêtre d'intervention annulée"),
            CancellationToken.None);

        run.Status.Should().Be(OperationRunStatus.Cancelled);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CompletedRun_ThrowsDomainRuleException()
    {
        var run = CreateApprovedRun();
        run.StartExecution();
        var stepId = run.StepExecutions[0].StepId;
        run.RecordStepStarted(stepId);
        run.RecordStepSucceeded(stepId, "OK");
        run.Complete();
        operationRuns.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new CancelOperationRunCommand(run.Id, "operateur@n4sentinel.local", "Motif"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainRuleException>();
    }

    [Fact]
    public async Task Handle_UnknownRun_ThrowsKeyNotFoundException()
    {
        operationRuns.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((OperationRun?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new CancelOperationRunCommand(Guid.NewGuid(), "operateur@n4sentinel.local", "Motif"), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
