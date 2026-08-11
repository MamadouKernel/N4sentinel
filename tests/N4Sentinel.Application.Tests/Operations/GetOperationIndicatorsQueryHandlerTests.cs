using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Operations.Queries;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Operations;

public class GetOperationIndicatorsQueryHandlerTests
{
    private readonly IOperationRunRepository operationRuns = Substitute.For<IOperationRunRepository>();

    private GetOperationIndicatorsQueryHandler CreateHandler() => new(operationRuns);

    private static OperationRun CreateRun(Guid environmentId, string stepName, WorkflowStepAction action, string? componentName = null) =>
        new(
            environmentId, Guid.NewGuid(), Guid.NewGuid(), 1, isProductionEnvironment: false,
            null, null, null, null, "operateur@n4sentinel.local",
            [(Guid.NewGuid(), 0, stepName, action, (Guid?)null, componentName)]);

    [Fact]
    public async Task Handle_NoRuns_ReturnsNullRatesRatherThanZero()
    {
        operationRuns.ListAllAsync(Arg.Any<CancellationToken>()).Returns(new List<OperationRun>());
        var handler = CreateHandler();

        var result = await handler.Handle(new GetOperationIndicatorsQuery(null), CancellationToken.None);

        result.TotalOperations.Should().Be(0);
        result.SuccessRatePercent.Should().BeNull();
        result.AverageDurationSeconds.Should().BeNull();
        result.SlowestSteps.Should().BeEmpty();
        result.RecurringErrors.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MixOfCompletedAndFailed_ComputesSuccessRate()
    {
        var environmentId = Guid.NewGuid();
        var completedRun = CreateRun(environmentId, "Démarrer le Bridge", WorkflowStepAction.Start, "Bridge");
        completedRun.StartExecution();
        var completedStepId = completedRun.StepExecutions[0].StepId;
        completedRun.RecordStepStarted(completedStepId);
        completedRun.RecordStepSucceeded(completedStepId, "OK");
        completedRun.Complete();

        var failedRun = CreateRun(environmentId, "Démarrer XPS", WorkflowStepAction.Start, "XPS");
        failedRun.StartExecution();
        var failedStepId = failedRun.StepExecutions[0].StepId;
        failedRun.RecordStepStarted(failedStepId);
        failedRun.RecordStepFailed(failedStepId, "Connexion refusée");
        failedRun.Fail();

        operationRuns.ListAllAsync(Arg.Any<CancellationToken>()).Returns(new List<OperationRun> { completedRun, failedRun });
        var handler = CreateHandler();

        var result = await handler.Handle(new GetOperationIndicatorsQuery(null), CancellationToken.None);

        result.TotalOperations.Should().Be(2);
        result.CompletedCount.Should().Be(1);
        result.FailedCount.Should().Be(1);
        result.SuccessRatePercent.Should().Be(50.0);
        result.AverageDurationSeconds.Should().NotBeNull();
        result.SlowestSteps.Should().ContainSingle(s => s.StepName == "Démarrer le Bridge");
        result.RecurringErrors.Should().ContainSingle(e => e.Message == "Connexion refusée" && e.Occurrences == 1);
    }

    [Fact]
    public async Task Handle_WithEnvironmentFilter_UsesListByEnvironment()
    {
        var environmentId = Guid.NewGuid();
        operationRuns.ListByEnvironmentAsync(environmentId, Arg.Any<CancellationToken>()).Returns(new List<OperationRun>());
        var handler = CreateHandler();

        await handler.Handle(new GetOperationIndicatorsQuery(environmentId), CancellationToken.None);

        await operationRuns.Received(1).ListByEnvironmentAsync(environmentId, Arg.Any<CancellationToken>());
        await operationRuns.DidNotReceiveWithAnyArgs().ListAllAsync(default);
    }
}
