using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Sequences;
using N4Sentinel.Application.Workflows.Commands;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Workflows;

/// <summary>FR-044 : blocage de l'activation d'une version dont l'ordre contredit la séquence active.</summary>
public class SequenceExceptionTests
{
    private readonly IWorkflowRepository workflows = Substitute.For<IWorkflowRepository>();
    private readonly ISequenceComplianceChecker compliance = Substitute.For<ISequenceComplianceChecker>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private ChangeWorkflowVersionStatusCommandHandler CreateHandler() => new(workflows, compliance, unitOfWork);

    /// <summary>Workflow amené jusqu'à l'état Validé, prêt à être activé.</summary>
    private static (Workflow Workflow, WorkflowVersion Version) ValidatedWorkflow()
    {
        var environmentId = Guid.NewGuid();
        var component = new N4Component(
            environmentId, "CENTER", "Center", ComponentCriticality.Critical, ComponentGovernance.Controllable,
            kind: N4ComponentKind.CenterNode);

        var workflow = new Workflow(environmentId, "Démarrage", WorkflowType.Start, WorkflowScope.Full, []);
        var version = workflow.LatestVersion;

        version.AddStep(
            "Démarrer CENTER", component.Id, WorkflowStepAction.Start, [], null, null, null, null,
            0, false, false, null, WorkflowStepFailurePolicy.StopWorkflow, false, false, false);

        workflow.SubmitVersionForValidation(version.Id);
        workflow.ValidateVersion(version.Id);

        return (workflow, version);
    }

    [Fact]
    public async Task Activate_WithOrderViolationAndNoException_IsBlocked()
    {
        var (workflow, version) = ValidatedWorkflow();
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        compliance.FindViolationsAsync(workflow, version, Arg.Any<CancellationToken>())
            .Returns(new[] { "Center Node placé avant les Cluster Nodes." });

        var act = () => CreateHandler().Handle(
            new ChangeWorkflowVersionStatusCommand(
                workflow.Id, version.Id, WorkflowVersionStatusAction.Activate, "admin"),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainRuleException>().WithMessage("*FR-044*");
        version.Status.Should().Be(WorkflowVersionStatus.Validated, "la version ne doit pas être activée");
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Activate_WithOrderViolationButApprovedException_IsAllowed()
    {
        var (workflow, version) = ValidatedWorkflow();
        version.ApproveSequenceException("Maintenance éditeur exceptionnelle", "operateur", "approbateur");

        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        compliance.FindViolationsAsync(workflow, version, Arg.Any<CancellationToken>())
            .Returns(new[] { "Center Node placé avant les Cluster Nodes." });

        await CreateHandler().Handle(
            new ChangeWorkflowVersionStatusCommand(
                workflow.Id, version.Id, WorkflowVersionStatusAction.Activate, "admin"),
            CancellationToken.None);

        version.Status.Should().Be(WorkflowVersionStatus.Active);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Activate_WithoutViolation_IsAllowed()
    {
        var (workflow, version) = ValidatedWorkflow();
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        compliance.FindViolationsAsync(workflow, version, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<string>());

        await CreateHandler().Handle(
            new ChangeWorkflowVersionStatusCommand(
                workflow.Id, version.Id, WorkflowVersionStatusAction.Activate, "admin"),
            CancellationToken.None);

        version.Status.Should().Be(WorkflowVersionStatus.Active);
    }

    [Fact]
    public async Task Validate_DoesNotTriggerSequenceCheck()
    {
        var environmentId = Guid.NewGuid();
        var workflow = new Workflow(environmentId, "Démarrage", WorkflowType.Start, WorkflowScope.Full, []);
        var version = workflow.LatestVersion;
        version.AddStep(
            "Étape", null, WorkflowStepAction.HealthCheck, [], null, null, null, null,
            0, false, false, null, WorkflowStepFailurePolicy.StopWorkflow, false, false, false);
        workflow.SubmitVersionForValidation(version.Id);

        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);

        await CreateHandler().Handle(
            new ChangeWorkflowVersionStatusCommand(
                workflow.Id, version.Id, WorkflowVersionStatusAction.Validate, "admin"),
            CancellationToken.None);

        // Le contrôle n'a de sens qu'au moment où le workflow devient exécutable.
        await compliance.DidNotReceiveWithAnyArgs().FindViolationsAsync(default!, default!, default);
    }

    [Fact]
    public void ApproveSequenceException_SameUserAsRequester_Throws()
    {
        var (_, version) = ValidatedWorkflow();

        var act = () => version.ApproveSequenceException("Motif", "jean", "JEAN");

        act.Should().Throw<DomainRuleException>().WithMessage("*distinct*");
    }

    [Fact]
    public void ApproveSequenceException_WithoutReason_Throws()
    {
        var (_, version) = ValidatedWorkflow();

        var act = () => version.ApproveSequenceException("  ", "operateur", "approbateur");

        act.Should().Throw<DomainRuleException>().WithMessage("*motif*");
    }

    [Fact]
    public void ApproveSequenceException_OnActiveVersion_Throws()
    {
        var (workflow, version) = ValidatedWorkflow();
        workflow.ActivateVersion(version.Id);

        var act = () => version.ApproveSequenceException("Motif", "operateur", "approbateur");

        act.Should().Throw<DomainRuleException>()
            .WithMessage("*avant l'activation*", "régulariser après coup viderait la règle de son sens");
    }
}
