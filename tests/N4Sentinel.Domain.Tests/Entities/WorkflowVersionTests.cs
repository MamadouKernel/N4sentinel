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
    public void UpdateStep_WithPrerequisitePositionedAfter_Throws()
    {
        var (_, draft) = CreateWorkflowWithDraft();
        var firstId = AddSimpleStep(draft, "Bridge Daemon");
        var secondId = AddSimpleStep(draft, "XPS");

        // "Bridge Daemon" est positionnée après "XPS" ne peut jamais dépendre de sa propre étape suivante :
        // on force l'ordre inverse pour tester la règle en réutilisant secondId comme prérequis de firstId.
        var act = () => draft.UpdateStep(
            firstId, "Bridge Daemon", null, WorkflowStepAction.Start, [secondId], null, null, null, null,
            maxRetryAttempts: 0, retryIsAutomatic: false, automaticRetryExplicitlyAuthorized: false,
            retryDelaySeconds: null, onFailurePolicy: WorkflowStepFailurePolicy.StopWorkflow,
            requiresConfirmation: false, requiresApproval: false, isCriticalOrDestructive: false);

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void UpdateStep_WithPrerequisitePositionedBefore_Succeeds()
    {
        var (_, draft) = CreateWorkflowWithDraft();
        var firstId = AddSimpleStep(draft, "Bridge Daemon");
        var secondId = AddSimpleStep(draft, "XPS");

        draft.UpdateStep(
            secondId, "XPS", null, WorkflowStepAction.Start, [firstId], null, null, null, null,
            maxRetryAttempts: 0, retryIsAutomatic: false, automaticRetryExplicitlyAuthorized: false,
            retryDelaySeconds: null, onFailurePolicy: WorkflowStepFailurePolicy.StopWorkflow,
            requiresConfirmation: false, requiresApproval: false, isCriticalOrDestructive: false);

        draft.Steps.Single(s => s.Id == secondId).PrerequisiteStepIds.Should().Contain(firstId);
    }

    [Fact]
    public void MoveStepUp_PastItsOwnPrerequisite_Throws()
    {
        var (_, draft) = CreateWorkflowWithDraft();
        var firstId = AddSimpleStep(draft, "Bridge Daemon");
        var secondId = AddSimpleStep(draft, "XPS", prerequisites: [firstId]);

        var act = () => draft.MoveStepUp(secondId);

        act.Should().Throw<DomainRuleException>();
        draft.Steps.Select(s => s.Name).Should().ContainInOrder("Bridge Daemon", "XPS");
    }

    [Fact]
    public void MoveStepDown_PastAStepThatDependsOnIt_Throws()
    {
        var (_, draft) = CreateWorkflowWithDraft();
        var firstId = AddSimpleStep(draft, "Bridge Daemon");
        AddSimpleStep(draft, "XPS", prerequisites: [firstId]);

        var act = () => draft.MoveStepDown(firstId);

        act.Should().Throw<DomainRuleException>();
        draft.Steps.Select(s => s.Name).Should().ContainInOrder("Bridge Daemon", "XPS");
    }

    [Fact]
    public void MoveStepUp_BetweenUnrelatedSteps_Succeeds()
    {
        var (_, draft) = CreateWorkflowWithDraft();
        AddSimpleStep(draft, "Étape 1");
        AddSimpleStep(draft, "Étape 2");
        var thirdId = AddSimpleStep(draft, "Étape 3");

        draft.MoveStepUp(thirdId);

        draft.Steps.Select(s => s.Name).Should().ContainInOrder("Étape 1", "Étape 3", "Étape 2");
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
