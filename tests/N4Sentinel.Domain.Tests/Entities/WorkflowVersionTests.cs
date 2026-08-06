using FluentAssertions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using Xunit;

namespace N4Sentinel.Domain.Tests.Entities;

public class WorkflowVersionTests
{
    private static (Workflow Workflow, WorkflowVersion Draft) CreateWorkflowWithDraft() =>
        CreateWorkflowFor(new Workflow(Guid.NewGuid(), "Démarrage complet", WorkflowType.Start, WorkflowScope.Full, []));

    private static (Workflow Workflow, WorkflowVersion Draft) CreateWorkflowFor(Workflow workflow) =>
        (workflow, workflow.LatestVersion);

    private static Guid AddSimpleStep(WorkflowVersion version, string name, IEnumerable<Guid>? prerequisites = null) =>
        version.AddStep(
            name, null, WorkflowStepAction.Start, prerequisites ?? [], null, null, null, null,
            maxRetryAttempts: 0, retryIsAutomatic: false, automaticRetryExplicitlyAuthorized: false,
            retryDelaySeconds: null, onFailurePolicy: WorkflowStepFailurePolicy.StopWorkflow,
            requiresConfirmation: false, requiresApproval: false, isCriticalOrDestructive: false).Id;

    [Fact]
    public void AddStep_AssignsSequentialPositions()
    {
        var (_, draft) = CreateWorkflowWithDraft();

        AddSimpleStep(draft, "Étape 1");
        AddSimpleStep(draft, "Étape 2");

        draft.Steps.Select(s => s.Name).Should().ContainInOrder("Étape 1", "Étape 2");
    }

    [Fact]
    public void AddStep_WithUnknownPrerequisite_Throws()
    {
        var (_, draft) = CreateWorkflowWithDraft();

        var act = () => AddSimpleStep(draft, "Étape", prerequisites: [Guid.NewGuid()]);

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void MoveStepUp_SwapsWithPreviousStep()
    {
        var (_, draft) = CreateWorkflowWithDraft();
        AddSimpleStep(draft, "Étape 1");
        var secondId = AddSimpleStep(draft, "Étape 2");

        draft.MoveStepUp(secondId);

        draft.Steps.Select(s => s.Name).Should().ContainInOrder("Étape 2", "Étape 1");
    }

    [Fact]
    public void MoveStepUp_OnFirstStep_IsNoOp()
    {
        var (_, draft) = CreateWorkflowWithDraft();
        var firstId = AddSimpleStep(draft, "Étape 1");
        AddSimpleStep(draft, "Étape 2");

        draft.MoveStepUp(firstId);

        draft.Steps.Select(s => s.Name).Should().ContainInOrder("Étape 1", "Étape 2");
    }

    [Fact]
    public void RemoveStep_WithDependents_Throws()
    {
        var (_, draft) = CreateWorkflowWithDraft();
        var firstId = AddSimpleStep(draft, "Étape 1");
        AddSimpleStep(draft, "Étape 2", prerequisites: [firstId]);

        var act = () => draft.RemoveStep(firstId);

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void SubmitForValidation_WithNoSteps_Throws()
    {
        var (workflow, draft) = CreateWorkflowWithDraft();

        var act = () => workflow.SubmitVersionForValidation(draft.Id);

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void FullValidationCycle_TransitionsThroughAllStatuses()
    {
        var (workflow, draft) = CreateWorkflowWithDraft();
        AddSimpleStep(draft, "Étape 1");

        workflow.SubmitVersionForValidation(draft.Id);
        draft.Status.Should().Be(WorkflowVersionStatus.PendingValidation);

        workflow.ValidateVersion(draft.Id);
        draft.Status.Should().Be(WorkflowVersionStatus.Validated);

        workflow.ActivateVersion(draft.Id);
        draft.Status.Should().Be(WorkflowVersionStatus.Active);
    }

    [Fact]
    public void AddStep_AfterVersionIsNoLongerDraft_Throws()
    {
        var (workflow, draft) = CreateWorkflowWithDraft();
        AddSimpleStep(draft, "Étape 1");
        workflow.SubmitVersionForValidation(draft.Id);

        var act = () => AddSimpleStep(draft, "Étape 2");

        act.Should().Throw<DomainRuleException>();
    }
}
