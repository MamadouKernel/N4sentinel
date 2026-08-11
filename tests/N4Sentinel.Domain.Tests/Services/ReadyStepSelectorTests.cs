using FluentAssertions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Services;
using Xunit;

namespace N4Sentinel.Domain.Tests.Services;

/// <summary>
/// FR-023 : « exécuter en parallèle uniquement les étapes explicitement déclarées comme indépendantes ».
/// </summary>
public class ReadyStepSelectorTests
{
    private static (Workflow Workflow, Guid Bridge, Guid Xps) CreateWorkflowWithTwoIndependentSteps()
    {
        var environmentId = Guid.NewGuid();
        var bridgeId = Guid.NewGuid();
        var xpsId = Guid.NewGuid();
        var workflow = new Workflow(environmentId, "Test", WorkflowType.Start, WorkflowScope.Full, []);
        var version = workflow.LatestVersion;

        version.AddStep(
            "Démarrer Bridge", bridgeId, WorkflowStepAction.Start, [], null, null, null, null, 0, false, false,
            null, WorkflowStepFailurePolicy.StopWorkflow, false, false, false);
        version.AddStep(
            "Démarrer XPS", xpsId, WorkflowStepAction.Start, [], null, null, null, null, 0, false, false,
            null, WorkflowStepFailurePolicy.StopWorkflow, false, false, false);

        workflow.SubmitVersionForValidation(version.Id);
        workflow.ValidateVersion(version.Id);
        workflow.ActivateVersion(version.Id);
        return (workflow, bridgeId, xpsId);
    }

    private static OperationRun CreateRunFor(Workflow workflow)
    {
        var version = workflow.ActiveVersion!;
        var steps = version.Steps.Select(s => (s.Id, s.Position, s.Name, s.Action, s.ComponentId, (string?)null));
        var run = new OperationRun(
            workflow.EnvironmentId, workflow.Id, version.Id, version.VersionNumber, isProductionEnvironment: false,
            null, null, null, null, "operateur@n4sentinel.local", steps);
        run.StartExecution();
        return run;
    }

    [Fact]
    public void TwoIndependentPendingSteps_BothReady()
    {
        var (workflow, _, _) = CreateWorkflowWithTwoIndependentSteps();
        var run = CreateRunFor(workflow);

        var ready = ReadyStepSelector.SelectReadySteps(run.StepExecutions, workflow.ActiveVersion!);

        ready.Should().HaveCount(2);
        ready.Should().BeEquivalentTo(run.StepExecutions.Select(s => s.StepId));
    }

    [Fact]
    public void SecondStepDependsOnFirst_OnlyFirstIsReady()
    {
        var environmentId = Guid.NewGuid();
        var bridgeId = Guid.NewGuid();
        var xpsId = Guid.NewGuid();
        var workflow = new Workflow(environmentId, "Test", WorkflowType.Start, WorkflowScope.Full, []);
        var version = workflow.LatestVersion;

        var bridgeStep = version.AddStep(
            "Démarrer Bridge", bridgeId, WorkflowStepAction.Start, [], null, null, null, null, 0, false, false,
            null, WorkflowStepFailurePolicy.StopWorkflow, false, false, false);
        version.AddStep(
            "Démarrer XPS", xpsId, WorkflowStepAction.Start, [bridgeStep.Id], null, null, null, null, 0, false,
            false, null, WorkflowStepFailurePolicy.StopWorkflow, false, false, false);

        workflow.SubmitVersionForValidation(version.Id);
        workflow.ValidateVersion(version.Id);
        workflow.ActivateVersion(version.Id);
        var run = CreateRunFor(workflow);

        var ready = ReadyStepSelector.SelectReadySteps(run.StepExecutions, workflow.ActiveVersion!);

        ready.Should().ContainSingle().Which.Should().Be(bridgeStep.Id);
    }

    [Fact]
    public void AfterPrerequisiteSucceeds_DependentStepBecomesReady()
    {
        var environmentId = Guid.NewGuid();
        var bridgeId = Guid.NewGuid();
        var xpsId = Guid.NewGuid();
        var workflow = new Workflow(environmentId, "Test", WorkflowType.Start, WorkflowScope.Full, []);
        var version = workflow.LatestVersion;

        var bridgeStep = version.AddStep(
            "Démarrer Bridge", bridgeId, WorkflowStepAction.Start, [], null, null, null, null, 0, false, false,
            null, WorkflowStepFailurePolicy.StopWorkflow, false, false, false);
        var xpsStep = version.AddStep(
            "Démarrer XPS", xpsId, WorkflowStepAction.Start, [bridgeStep.Id], null, null, null, null, 0, false,
            false, null, WorkflowStepFailurePolicy.StopWorkflow, false, false, false);

        workflow.SubmitVersionForValidation(version.Id);
        workflow.ValidateVersion(version.Id);
        workflow.ActivateVersion(version.Id);
        var run = CreateRunFor(workflow);
        run.RecordStepStarted(bridgeStep.Id);
        run.RecordStepSucceeded(bridgeStep.Id, "OK");

        var ready = ReadyStepSelector.SelectReadySteps(run.StepExecutions, workflow.ActiveVersion!);

        ready.Should().ContainSingle().Which.Should().Be(xpsStep.Id);
    }

    [Fact]
    public void TwoStepsTargetingSameComponent_OnlyFirstIsReady()
    {
        var environmentId = Guid.NewGuid();
        var componentId = Guid.NewGuid();
        var workflow = new Workflow(environmentId, "Test", WorkflowType.Start, WorkflowScope.Full, []);
        var version = workflow.LatestVersion;

        var first = version.AddStep(
            "Vérifier composant", componentId, WorkflowStepAction.HealthCheck, [], null, null, null, null, 0,
            false, false, null, WorkflowStepFailurePolicy.StopWorkflow, false, false, false);
        version.AddStep(
            "Redémarrer composant", componentId, WorkflowStepAction.Restart, [], null, null, null, null, 0,
            false, false, null, WorkflowStepFailurePolicy.StopWorkflow, false, false, false);

        workflow.SubmitVersionForValidation(version.Id);
        workflow.ValidateVersion(version.Id);
        workflow.ActivateVersion(version.Id);
        var run = CreateRunFor(workflow);

        var ready = ReadyStepSelector.SelectReadySteps(run.StepExecutions, workflow.ActiveVersion!);

        ready.Should().ContainSingle().Which.Should().Be(first.Id);
    }

    [Fact]
    public void NoPendingSteps_ReturnsEmpty()
    {
        var (workflow, _, _) = CreateWorkflowWithTwoIndependentSteps();
        var run = CreateRunFor(workflow);
        foreach (var step in run.StepExecutions)
        {
            run.RecordStepStarted(step.StepId);
            run.RecordStepSucceeded(step.StepId, "OK");
        }

        var ready = ReadyStepSelector.SelectReadySteps(run.StepExecutions, workflow.ActiveVersion!);

        ready.Should().BeEmpty();
    }

    [Fact]
    public void N4SequentialChain_NeverReturnsMoreThanOneAtATime()
    {
        // Reflète la contrainte N4 réelle (Cluster Nodes/Center/Bridge/XPS/ECN4) : chaque étape générée par
        // SequencePlanner pour un palier Sequential dépend de la précédente.
        var environmentId = Guid.NewGuid();
        var cluster1Id = Guid.NewGuid();
        var cluster2Id = Guid.NewGuid();
        var workflow = new Workflow(environmentId, "Test", WorkflowType.Start, WorkflowScope.Full, []);
        var version = workflow.LatestVersion;

        var step1 = version.AddStep(
            "Démarrer Cluster 1", cluster1Id, WorkflowStepAction.Start, [], null, null, null, null, 0, false,
            false, null, WorkflowStepFailurePolicy.StopWorkflow, false, false, false);
        version.AddStep(
            "Démarrer Cluster 2", cluster2Id, WorkflowStepAction.Start, [step1.Id], null, null, null, null, 0,
            false, false, null, WorkflowStepFailurePolicy.StopWorkflow, false, false, false);

        workflow.SubmitVersionForValidation(version.Id);
        workflow.ValidateVersion(version.Id);
        workflow.ActivateVersion(version.Id);
        var run = CreateRunFor(workflow);

        ReadyStepSelector.SelectReadySteps(run.StepExecutions, workflow.ActiveVersion!).Should().HaveCount(1);
    }
}
