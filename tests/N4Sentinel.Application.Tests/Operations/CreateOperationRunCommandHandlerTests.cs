using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Operations.Commands;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Operations;

public class CreateOperationRunCommandHandlerTests
{
    private readonly IEnvironmentRepository environments = Substitute.For<IEnvironmentRepository>();
    private readonly IWorkflowRepository workflows = Substitute.For<IWorkflowRepository>();
    private readonly IComponentRepository components = Substitute.For<IComponentRepository>();
    private readonly IOperationRunRepository operationRuns = Substitute.For<IOperationRunRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private CreateOperationRunCommandHandler CreateHandler() =>
        new(environments, workflows, components, operationRuns, unitOfWork);

    private static Workflow CreateActiveWorkflow()
    {
        var workflow = new Workflow(Guid.NewGuid(), "Démarrage complet", WorkflowType.Start, WorkflowScope.Full, []);
        var version = workflow.LatestVersion;
        version.AddStep(
            "Démarrer le Bridge", null, WorkflowStepAction.Start, [], null, null, null, null, 0, false, false,
            null, WorkflowStepFailurePolicy.StopWorkflow, false, false, false);
        workflow.SubmitVersionForValidation(version.Id);
        workflow.ValidateVersion(version.Id);
        workflow.ActivateVersion(version.Id);
        return workflow;
    }

    [Fact]
    public async Task Handle_ProductionEnvironmentWithoutMotif_ThrowsDomainRuleException()
    {
        var environment = new N4Environment("Production", "PROD", EnvironmentKind.Production, null);
        var workflow = CreateActiveWorkflow();
        environments.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new CreateOperationRunCommand(
                environment.Id, workflow.Id, workflow.ActiveVersion!.Id, null, null, null, null,
                "operateur@n4sentinel.local"),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainRuleException>();
    }

    [Fact]
    public async Task Handle_NonProductionEnvironment_CreatesApprovedRun()
    {
        var environment = new N4Environment("UAT", "UAT", EnvironmentKind.Uat, null);
        var workflow = CreateActiveWorkflow();
        environments.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        var handler = CreateHandler();

        var id = await handler.Handle(
            new CreateOperationRunCommand(
                environment.Id, workflow.Id, workflow.ActiveVersion!.Id, null, null, null, null,
                "operateur@n4sentinel.local"),
            CancellationToken.None);

        id.Should().NotBeEmpty();
        operationRuns.Received(1).Add(Arg.Is<OperationRun>(r => r!.Status == OperationRunStatus.Approved));
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DraftVersion_ThrowsValidationException()
    {
        var environment = new N4Environment("UAT", "UAT", EnvironmentKind.Uat, null);
        var workflow = new Workflow(environment.Id, "Démarrage complet", WorkflowType.Start, WorkflowScope.Full, []);
        environments.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new CreateOperationRunCommand(
                environment.Id, workflow.Id, workflow.LatestVersion.Id, null, null, null, null,
                "operateur@n4sentinel.local"),
            CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }
}
