using FluentAssertions;
using Microsoft.Extensions.Logging;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Operations;
using N4Sentinel.Application.Operations.Commands;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Operations;

public class ConfirmOperationStepCommandHandlerTests
{
    private readonly IOperationRunRepository operationRuns = Substitute.For<IOperationRunRepository>();
    private readonly IWorkflowRepository workflows = Substitute.For<IWorkflowRepository>();
    private readonly IComponentRepository components = Substitute.For<IComponentRepository>();
    private readonly IServerConnector connector = Substitute.For<IServerConnector>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ILogger<ConfirmOperationStepCommandHandler> logger =
        Substitute.For<ILogger<ConfirmOperationStepCommandHandler>>();

    private ConfirmOperationStepCommandHandler CreateHandler() =>
        new(operationRuns, workflows, new OperationStepExecutionService(connector, components), unitOfWork, logger);

    [Fact]
    public async Task Handle_AwaitingConfirmationStep_ExecutesAndCompletesRun()
    {
        var environmentId = Guid.NewGuid();
        var component = new N4Component(
            environmentId, "Bridge", "Bridge daemon", ComponentCriticality.Critical, ComponentGovernance.Controllable);
        var workflow = new Workflow(environmentId, "Démarrage complet", WorkflowType.Start, WorkflowScope.Full, []);
        var version = workflow.LatestVersion;
        version.AddStep(
            "Démarrer le Bridge", component.Id, WorkflowStepAction.Start, [], null, null, null, null, 0, false,
            false, null, WorkflowStepFailurePolicy.StopWorkflow, true, false, false);
        workflow.SubmitVersionForValidation(version.Id);
        workflow.ValidateVersion(version.Id);
        workflow.ActivateVersion(version.Id);

        var steps = version.Steps.Select(s => (s.Id, s.Position, s.Name, s.Action, s.ComponentId, (string?)null));
        var run = new OperationRun(
            environmentId, workflow.Id, version.Id, version.VersionNumber, isProductionEnvironment: false,
            null, null, null, null, "operateur@n4sentinel.local", steps);
        run.StartExecution();
        var stepId = run.StepExecutions[0].StepId;
        run.RecordStepAwaitingConfirmation(stepId);

        operationRuns.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        components.GetByIdAsync(component.Id, Arg.Any<CancellationToken>()).Returns(component);
        connector.StartAsync(component, Arg.Any<CancellationToken>()).Returns(new ServerActionResult(true, "OK"));
        var handler = CreateHandler();

        await handler.Handle(
            new ConfirmOperationStepCommand(run.Id, stepId, "approbateur@n4sentinel.local"), CancellationToken.None);

        run.StepExecutions[0].Status.Should().Be(OperationStepExecutionStatus.Succeeded);
        run.Status.Should().Be(OperationRunStatus.Completed);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ApprovalRequiredStep_ConfirmedByRequester_Throws()
    {
        var environmentId = Guid.NewGuid();
        var component = new N4Component(
            environmentId, "Bridge", "Bridge daemon", ComponentCriticality.Critical, ComponentGovernance.Controllable);
        var workflow = new Workflow(environmentId, "Démarrage complet", WorkflowType.Start, WorkflowScope.Full, []);
        var version = workflow.LatestVersion;
        version.AddStep(
            "Démarrer le Bridge", component.Id, WorkflowStepAction.Start, [], null, null, null, null, 0, false,
            false, null, WorkflowStepFailurePolicy.StopWorkflow, false, true, false);
        workflow.SubmitVersionForValidation(version.Id);
        workflow.ValidateVersion(version.Id);
        workflow.ActivateVersion(version.Id);

        var steps = version.Steps.Select(s => (s.Id, s.Position, s.Name, s.Action, s.ComponentId, (string?)null));
        var run = new OperationRun(
            environmentId, workflow.Id, version.Id, version.VersionNumber, isProductionEnvironment: false,
            null, null, null, null, "operateur@n4sentinel.local", steps);
        run.StartExecution();
        var stepId = run.StepExecutions[0].StepId;
        run.RecordStepAwaitingConfirmation(stepId);

        operationRuns.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new ConfirmOperationStepCommand(run.Id, stepId, "operateur@n4sentinel.local"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainRuleException>();
        await connector.DidNotReceiveWithAnyArgs().StartAsync(default!, default);
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }
}
