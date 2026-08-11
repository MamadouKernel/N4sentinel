using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Operations.Commands;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Operations;

public class ResumeOperationRunCommandHandlerTests
{
    private readonly IOperationRunRepository operationRuns = Substitute.For<IOperationRunRepository>();
    private readonly IComponentRepository components = Substitute.For<IComponentRepository>();
    private readonly IServerConnector connector = Substitute.For<IServerConnector>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private ResumeOperationRunCommandHandler CreateHandler() => new(operationRuns, components, connector, unitOfWork);

    private static OperationRun CreateFailedRun(Guid? componentId = null, WorkflowStepAction action = WorkflowStepAction.Start)
    {
        var steps = new[] { (Guid.NewGuid(), 0, "Démarrer le Bridge", action, componentId, (string?)"Bridge") };
        var run = new OperationRun(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, isProductionEnvironment: false,
            null, null, null, null, "operateur@n4sentinel.local", steps);
        run.StartExecution();
        var stepId = run.StepExecutions[0].StepId;
        run.RecordStepStarted(stepId);
        run.RecordStepFailed(stepId, "Erreur");
        run.Fail();
        return run;
    }

    [Fact]
    public async Task Handle_FailedRunWithoutComponent_ResumesAndSaves()
    {
        var run = CreateFailedRun();
        operationRuns.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);
        var handler = CreateHandler();

        await handler.Handle(new ResumeOperationRunCommand(run.Id), CancellationToken.None);

        run.Status.Should().Be(OperationRunStatus.Running);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComponentStillDown_ResumesNormally()
    {
        var component = new N4Component(
            Guid.NewGuid(), "Bridge", "Bridge daemon", ComponentCriticality.Critical, ComponentGovernance.Controllable);
        var run = CreateFailedRun(component.Id);
        operationRuns.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);
        components.GetByIdAsync(component.Id, Arg.Any<CancellationToken>()).Returns(component);
        connector.CheckHealthAsync(component, Arg.Any<CancellationToken>()).Returns(ComponentHealthStatus.Shutdown);
        var handler = CreateHandler();

        await handler.Handle(new ResumeOperationRunCommand(run.Id), CancellationToken.None);

        run.Status.Should().Be(OperationRunStatus.Running);
    }

    [Fact]
    public async Task Handle_StartStepButComponentAlreadyActive_FlagsReconciliationRequired()
    {
        var component = new N4Component(
            Guid.NewGuid(), "Bridge", "Bridge daemon", ComponentCriticality.Critical, ComponentGovernance.Controllable);
        var run = CreateFailedRun(component.Id);
        operationRuns.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);
        components.GetByIdAsync(component.Id, Arg.Any<CancellationToken>()).Returns(component);
        connector.CheckHealthAsync(component, Arg.Any<CancellationToken>()).Returns(ComponentHealthStatus.Active);
        var handler = CreateHandler();

        await handler.Handle(new ResumeOperationRunCommand(run.Id), CancellationToken.None);

        run.Status.Should().Be(OperationRunStatus.ReconciliationRequired);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConnectorThrows_ResumesNormallyRatherThanBlocking()
    {
        var component = new N4Component(
            Guid.NewGuid(), "Bridge", "Bridge daemon", ComponentCriticality.Critical, ComponentGovernance.Controllable);
        var run = CreateFailedRun(component.Id);
        operationRuns.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);
        components.GetByIdAsync(component.Id, Arg.Any<CancellationToken>()).Returns(component);
        connector.CheckHealthAsync(component, Arg.Any<CancellationToken>())
            .Returns<ComponentHealthStatus>(_ => throw new InvalidOperationException("Connecteur indisponible"));
        var handler = CreateHandler();

        await handler.Handle(new ResumeOperationRunCommand(run.Id), CancellationToken.None);

        run.Status.Should().Be(OperationRunStatus.Running);
    }

    [Fact]
    public async Task Handle_NonFailedRun_ThrowsDomainRuleException()
    {
        var steps = new[] { (Guid.NewGuid(), 0, "Démarrer le Bridge", WorkflowStepAction.Start, (Guid?)null, (string?)null) };
        var run = new OperationRun(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, isProductionEnvironment: false,
            null, null, null, null, "operateur@n4sentinel.local", steps);
        operationRuns.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);
        var handler = CreateHandler();

        var act = () => handler.Handle(new ResumeOperationRunCommand(run.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainRuleException>();
    }
}
