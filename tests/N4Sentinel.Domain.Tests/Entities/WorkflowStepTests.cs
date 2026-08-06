using FluentAssertions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using Xunit;

namespace N4Sentinel.Domain.Tests.Entities;

public class WorkflowStepTests
{
    private static WorkflowStep CreateStep(bool isCritical = false, bool retryAutomatic = false, bool retryAuthorized = false) =>
        new(
            "Démarrer le Bridge", null, WorkflowStepAction.Start, "Service démarré", 30, 60, 120,
            maxRetryAttempts: retryAutomatic ? 1 : 0, retryIsAutomatic: retryAutomatic,
            automaticRetryExplicitlyAuthorized: retryAuthorized, retryDelaySeconds: 10,
            onFailurePolicy: WorkflowStepFailurePolicy.StopWorkflow, requiresConfirmation: false,
            requiresApproval: false, isCriticalOrDestructive: isCritical);

    [Fact]
    public void Constructor_WithEmptyName_Throws()
    {
        var act = () => new WorkflowStep(
            "", null, WorkflowStepAction.Start, null, null, null, null, 0, false, false, null,
            WorkflowStepFailurePolicy.StopWorkflow, false, false, false);

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Constructor_WithNegativeMaxRetryAttempts_Throws()
    {
        var act = () => new WorkflowStep(
            "Étape", null, WorkflowStepAction.Start, null, null, null, null, -1, false, false, null,
            WorkflowStepFailurePolicy.StopWorkflow, false, false, false);

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Constructor_CriticalWithAutomaticRetryNotAuthorized_Throws()
    {
        var act = () => CreateStep(isCritical: true, retryAutomatic: true, retryAuthorized: false);

        act.Should().Throw<DomainRuleException>()
            .WithMessage("*interdites*");
    }

    [Fact]
    public void Constructor_CriticalWithAutomaticRetryExplicitlyAuthorized_Succeeds()
    {
        var step = CreateStep(isCritical: true, retryAutomatic: true, retryAuthorized: true);

        step.RetryIsAutomatic.Should().BeTrue();
        step.AutomaticRetryExplicitlyAuthorized.Should().BeTrue();
    }

    [Fact]
    public void AddDependency_ReplacePrerequisites_WithSelf_Throws()
    {
        var step = CreateStep();

        var act = () => step.ReplacePrerequisites([step.Id]);

        act.Should().Throw<DomainRuleException>();
    }
}
