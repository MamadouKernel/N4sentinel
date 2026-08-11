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
    private readonly ISharedFolderRepository sharedFolders = Substitute.For<ISharedFolderRepository>();
    private readonly ISyncEndpointRepository syncEndpoints = Substitute.For<ISyncEndpointRepository>();
    private readonly IEdiFileRepository ediFiles = Substitute.For<IEdiFileRepository>();

    private GetDashboardSummaryQueryHandler CreateHandler()
    {
        sharedFolders.ListAllAsync(Arg.Any<CancellationToken>()).Returns([]);
        syncEndpoints.ListAllAsync(Arg.Any<CancellationToken>()).Returns([]);
        ediFiles.ListAllAsync(Arg.Any<CancellationToken>()).Returns([]);
        return new(environments, operationRuns, sharedFolders, syncEndpoints, ediFiles);
    }

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
            case OperationRunStatus.ReconciliationRequired:
                run.StartExecution();
                var reconciliationStepId = run.StepExecutions[0].StepId;
                run.RecordStepStarted(reconciliationStepId);
                run.RecordStepFailed(reconciliationStepId, "Erreur");
                run.Fail();
                run.FlagReconciliationRequired("Écart constaté");
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

    [Fact]
    public async Task Handle_ReconciliationRequiredRun_CountsAsActiveAndSurfacesAlert()
    {
        var environment = new N4Environment("Production", "PROD", EnvironmentKind.Production, null);
        environments.ListAllAsync(Arg.Any<CancellationToken>()).Returns([environment]);

        var reconciliation = CreateRunWithStatus(environment.Id, OperationRunStatus.ReconciliationRequired);
        operationRuns.ListAllAsync(Arg.Any<CancellationToken>()).Returns([reconciliation]);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        result.ActiveOperations.Should().ContainSingle().Which.Id.Should().Be(reconciliation.Id);
        result.ReconciliationRequiredAlert.Should().ContainSingle().Which.Id.Should().Be(reconciliation.Id);
        result.FailedOperationsAlert.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_IncludesSharedFolderAndSyncEndpointAnomaliesInSupervisionAlerts()
    {
        var environment = new N4Environment("Production", "PROD", EnvironmentKind.Production, null);
        environments.ListAllAsync(Arg.Any<CancellationToken>()).Returns([environment]);
        operationRuns.ListAllAsync(Arg.Any<CancellationToken>()).Returns([]);

        var healthyFolder = new SharedFolder(environment.Id, "Config", SharedFolderCategory.Configuration, @"C:\config");
        var anomalousFolder = new SharedFolder(environment.Id, "AMQ Store", SharedFolderCategory.ActiveMqKahaDb, @"C:\amq");
        anomalousFolder.RecordHealthCheck(false, 50, true, CorruptionStatus.None, "Inaccessible");
        sharedFolders.ListAllAsync(Arg.Any<CancellationToken>()).Returns([healthyFolder, anomalousFolder]);

        var anomalousEndpoint = new SyncEndpoint(environment.Id, "Bridge Queue");
        anomalousEndpoint.RecordSyncCheck(2000, 1, DateTime.UtcNow, "File trop longue");
        syncEndpoints.ListAllAsync(Arg.Any<CancellationToken>()).Returns([anomalousEndpoint]);

        var rejectedEdiFile = new EdiFile(environment.Id, "BAPLIE", "Armateur X");
        rejectedEdiFile.MarkRejected("Format non conforme");
        ediFiles.ListAllAsync(Arg.Any<CancellationToken>()).Returns([rejectedEdiFile]);

        var handler = new GetDashboardSummaryQueryHandler(environments, operationRuns, sharedFolders, syncEndpoints, ediFiles);

        var result = await handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        result.SupervisionAlerts.Should().HaveCount(3);
        result.SupervisionAlerts.Should().Contain(a => a.Name == "AMQ Store" && a.Kind == "Dossier partagé");
        result.SupervisionAlerts.Should().Contain(a => a.Name == "Bridge Queue" && a.Kind == "Synchronisation");
        result.SupervisionAlerts.Should().Contain(a => a.Kind == "EDI" && a.Name == "BAPLIE — Armateur X");
    }
}
