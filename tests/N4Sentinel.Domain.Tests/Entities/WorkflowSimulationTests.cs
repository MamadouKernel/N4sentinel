using FluentAssertions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using Xunit;

namespace N4Sentinel.Domain.Tests.Entities;

public class WorkflowSimulationTests
{
    private static WorkflowSimulationStepResult CreateStepResult(
        bool canExecute = true, bool requiresConfirmation = false, bool requiresApproval = false) =>
        new(
            Guid.NewGuid(), 0, "Démarrer le Bridge", WorkflowStepAction.Start, Guid.NewGuid(), "Bridge",
            ComponentHealthStatus.Active, canExecute, canExecute ? null : "Composant non pilotable.",
            requiresConfirmation, requiresApproval, isCriticalOrDestructive: false, expectedDurationSeconds: 30);

    [Fact]
    public void Constructor_WithEmptyRequestedByUserId_Throws()
    {
        var act = () => new WorkflowSimulation(
            Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(), "", [CreateStepResult()]);

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void HasBlockingIssues_WithAllStepsExecutable_IsFalse()
    {
        var simulation = new WorkflowSimulation(
            Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(), "admin@n4sentinel.local",
            [CreateStepResult(canExecute: true)]);

        simulation.HasBlockingIssues.Should().BeFalse();
    }

    [Fact]
    public void HasBlockingIssues_WithAnyNonExecutableStep_IsTrue()
    {
        var simulation = new WorkflowSimulation(
            Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(), "admin@n4sentinel.local",
            [CreateStepResult(canExecute: true), CreateStepResult(canExecute: false)]);

        simulation.HasBlockingIssues.Should().BeTrue();
    }

    [Fact]
    public void RequiresHumanValidation_WithConfirmationOrApprovalStep_IsTrue()
    {
        var simulation = new WorkflowSimulation(
            Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(), "admin@n4sentinel.local",
            [CreateStepResult(requiresApproval: true)]);

        simulation.RequiresHumanValidation.Should().BeTrue();
    }

    [Fact]
    public void RequiresHumanValidation_WithNoConfirmationOrApprovalStep_IsFalse()
    {
        var simulation = new WorkflowSimulation(
            Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(), "admin@n4sentinel.local",
            [CreateStepResult()]);

        simulation.RequiresHumanValidation.Should().BeFalse();
    }
}
