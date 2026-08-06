using FluentAssertions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using Xunit;

namespace N4Sentinel.Domain.Tests.Entities;

public class WorkflowTests
{
    [Fact]
    public void Constructor_WithEmptyName_Throws()
    {
        var act = () => new Workflow(Guid.NewGuid(), "", WorkflowType.Start, WorkflowScope.Full, []);

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Constructor_PartialScopeWithoutTargets_Throws()
    {
        var act = () => new Workflow(Guid.NewGuid(), "Redémarrage Bridge", WorkflowType.Restart, WorkflowScope.Partial, []);

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Constructor_CreatesInitialDraftVersion()
    {
        var workflow = new Workflow(Guid.NewGuid(), "Démarrage complet", WorkflowType.Start, WorkflowScope.Full, []);

        workflow.Versions.Should().ContainSingle();
        workflow.LatestVersion.VersionNumber.Should().Be(1);
        workflow.LatestVersion.Status.Should().Be(WorkflowVersionStatus.Draft);
    }

    [Fact]
    public void CreateNewDraftVersion_WhenLatestIsDraft_Throws()
    {
        var workflow = new Workflow(Guid.NewGuid(), "Démarrage complet", WorkflowType.Start, WorkflowScope.Full, []);

        var act = workflow.CreateNewDraftVersion;

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void CreateNewDraftVersion_ClonesStepsFromLatestVersion()
    {
        var workflow = new Workflow(Guid.NewGuid(), "Démarrage complet", WorkflowType.Start, WorkflowScope.Full, []);
        var v1 = workflow.LatestVersion;
        v1.AddStep(
            "Démarrer Cluster Node 1", null, WorkflowStepAction.Start, [], null, null, null, null, 0, false,
            false, null, WorkflowStepFailurePolicy.StopWorkflow, false, false, false);
        workflow.SubmitVersionForValidation(v1.Id);
        workflow.ValidateVersion(v1.Id);
        workflow.ActivateVersion(v1.Id);

        var v2 = workflow.CreateNewDraftVersion();

        v2.VersionNumber.Should().Be(2);
        v2.Status.Should().Be(WorkflowVersionStatus.Draft);
        v2.Steps.Should().ContainSingle(s => s.Name == "Démarrer Cluster Node 1");
    }

    [Fact]
    public void ActivateVersion_DisablesPreviouslyActiveVersion()
    {
        var workflow = new Workflow(Guid.NewGuid(), "Démarrage complet", WorkflowType.Start, WorkflowScope.Full, []);
        var v1 = workflow.LatestVersion;
        v1.AddStep(
            "Étape", null, WorkflowStepAction.Start, [], null, null, null, null, 0, false, false, null,
            WorkflowStepFailurePolicy.StopWorkflow, false, false, false);
        workflow.SubmitVersionForValidation(v1.Id);
        workflow.ValidateVersion(v1.Id);
        workflow.ActivateVersion(v1.Id);

        var v2 = workflow.CreateNewDraftVersion();
        workflow.SubmitVersionForValidation(v2.Id);
        workflow.ValidateVersion(v2.Id);
        workflow.ActivateVersion(v2.Id);

        v1.Status.Should().Be(WorkflowVersionStatus.Disabled);
        v2.Status.Should().Be(WorkflowVersionStatus.Active);
        workflow.ActiveVersion.Should().Be(v2);
    }
}
