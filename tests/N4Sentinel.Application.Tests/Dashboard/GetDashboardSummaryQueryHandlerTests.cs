using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Dashboard.Queries;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Dashboard;

public class GetDashboardSummaryQueryHandlerTests
{
    private readonly IEnvironmentRepository environments = Substitute.For<IEnvironmentRepository>();
    private readonly IOperationRunRepository operationRuns = Substitute.For<IOperationRunRepository>();

    private GetDashboardSummaryQueryHandler CreateHandler() => new(environments, operationRuns);

    private static OperationRun CreateRunWithStatus(Guid environmentId, OperationRunStatus status)
    {
        var isProduction = status == OperationRunStatus.PendingApproval;
        var run = new OperationRun(
            environmentId, Guid.NewGuid(), Guid.NewGuid(), 1, isProductionEnvironment: isProduction,
            isProduction ? "Maintenance planifiée" : null, isProduction ? "22:00-23:00" : null,
            isProduction ? "Aucun" : null, isProduction ? "CHG-1" : null, "operateur@n4sentinel.local",
            [(Guid.NewGuid(), 0, "Démarrer le Bridge", WorkflowStepAction.Start, (Guid?)null, (string?)null)]);

        switch (status)
        {
            case OperationRunStatus.PendingApproval:
            case OperationRunStatus.Approved:
                break;
            case OperationRunStatus.Running:
                run.StartExecution();
                break;
            case OperationRunStatus.Failed:
                run.StartExecution();
                var stepId = run.StepExecutions[0].StepId;
                run.RecordStepStarted(stepId);
                run.RecordStepFailed(stepId, "Erreur");
                run.Fail();
                break;
        }

        return run;
    }

    [Fact]
    public async Task Handle_ClassifiesRunsIntoActiveFailedAndPendingApprovalCount()
    {
        var environment = new N4Environment("Production", "PROD", EnvironmentKind.Production, null);
        environments.ListAllAsync(Arg.Any<CancellationToken>()).Returns([environment]);

        var pending = CreateRunWithStatus(environment.Id, OperationRunStatus.PendingApproval);
        var running = CreateRunWithStatus(environment.Id, OperationRunStatus.Running);
        var failed = CreateRunWithStatus(environment.Id, OperationRunStatus.Failed);
        operationRuns.ListAllAsync(Arg.Any<CancellationToken>()).Returns([pending, running, failed]);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        result.ActiveOperations.Should().HaveCount(2);
        result.ActiveOperations.Select(r => r.Id).Should().BeEquivalentTo([pending.Id, running.Id]);
        result.FailedOperationsAlert.Should().ContainSingle().Which.Id.Should().Be(failed.Id);
        result.PendingApprovalsCount.Should().Be(1);
        result.Environments.Should().ContainSingle().Which.ActiveOperationsCount.Should().Be(2);
    }
}
