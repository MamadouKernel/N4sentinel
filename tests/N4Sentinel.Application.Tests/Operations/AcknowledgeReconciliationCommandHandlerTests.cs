using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Operations.Commands;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Operations;

public class AcknowledgeReconciliationCommandHandlerTests
{
    private readonly IOperationRunRepository operationRuns = Substitute.For<IOperationRunRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private AcknowledgeReconciliationCommandHandler CreateHandler() => new(operationRuns, unitOfWork);

    private static OperationRun CreateReconciliationRequiredRun()
    {
        var steps = new[] { (Guid.NewGuid(), 0, "Démarrer le Bridge", WorkflowStepAction.Start, (Guid?)null, (string?)null) };
        var run = new OperationRun(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, isProductionEnvironment: false,
            null, null, null, null, "operateur@n4sentinel.local", steps);
        run.StartExecution();
        var stepId = run.StepExecutions[0].StepId;
        run.RecordStepStarted(stepId);
        run.RecordStepFailed(stepId, "Erreur");
        run.Fail();
        run.FlagReconciliationRequired("Écart constaté");
        return run;
    }

    [Fact]
    public async Task Handle_ReconciliationRequiredRun_ReturnsToFailedAndSaves()
    {
        var run = CreateReconciliationRequiredRun();
        operationRuns.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);
        var handler = CreateHandler();

        await handler.Handle(
            new AcknowledgeReconciliationCommand(run.Id, "operateur@n4sentinel.local"), CancellationToken.None);

        run.Status.Should().Be(OperationRunStatus.Failed);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NotFlagged_ThrowsDomainRuleException()
    {
        var steps = new[] { (Guid.NewGuid(), 0, "Démarrer le Bridge", WorkflowStepAction.Start, (Guid?)null, (string?)null) };
        var run = new OperationRun(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, isProductionEnvironment: false,
            null, null, null, null, "operateur@n4sentinel.local", steps);
        operationRuns.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new AcknowledgeReconciliationCommand(run.Id, "operateur@n4sentinel.local"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainRuleException>();
    }
}
