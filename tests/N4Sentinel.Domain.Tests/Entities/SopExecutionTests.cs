using FluentAssertions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using Xunit;

namespace N4Sentinel.Domain.Tests.Entities;

public class SopExecutionTests
{
    private static SopExecution CreateExecution() => new(Guid.NewGuid(), 1, "operateur1");

    [Fact]
    public void Constructor_WithEmptyStartedByUserId_Throws()
    {
        var act = () => new SopExecution(Guid.NewGuid(), 1, "");

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void NewExecution_IsInProgress_WithNoConfirmations()
    {
        var execution = CreateExecution();

        execution.Status.Should().Be(SopExecutionStatus.InProgress);
        execution.StepConfirmations.Should().BeEmpty();
    }

    [Fact]
    public void ConfirmNextStep_AppendsConfirmationInOrder()
    {
        var execution = CreateExecution();

        execution.ConfirmNextStep("Arrêter le service", "operateur1", "capture.png", null);
        execution.ConfirmNextStep("Vérifier les logs", "operateur1", null, "Log inhabituel mais accepté");

        execution.StepConfirmations.Should().HaveCount(2);
        execution.StepConfirmations[0].StepIndex.Should().Be(0);
        execution.StepConfirmations[1].StepIndex.Should().Be(1);
        execution.StepConfirmations[1].IsDeviation.Should().BeTrue();
    }

    [Fact]
    public void GoBackOneStep_RemovesLastConfirmation()
    {
        var execution = CreateExecution();
        execution.ConfirmNextStep("Arrêter le service", "operateur1", null, null);
        execution.ConfirmNextStep("Vérifier les logs", "operateur1", null, null);

        execution.GoBackOneStep();

        execution.StepConfirmations.Should().ContainSingle().Which.StepText.Should().Be("Arrêter le service");
    }

    [Fact]
    public void GoBackOneStep_WithNoConfirmations_Throws()
    {
        var execution = CreateExecution();

        var act = () => execution.GoBackOneStep();

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Complete_SetsResolvedIssueAndStatus()
    {
        var execution = CreateExecution();
        execution.ConfirmNextStep("Arrêter le service", "operateur1", null, null);

        execution.Complete(resolvedIssue: true);

        execution.Status.Should().Be(SopExecutionStatus.Completed);
        execution.ResolvedIssue.Should().BeTrue();
        execution.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void ConfirmNextStep_AfterCompletion_Throws()
    {
        var execution = CreateExecution();
        execution.Complete(resolvedIssue: false);

        var act = () => execution.ConfirmNextStep("Étape", "operateur1", null, null);

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Abort_WithEmptyReason_Throws()
    {
        var execution = CreateExecution();

        var act = () => execution.Abort("");

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Abort_SetsStatusAndReason()
    {
        var execution = CreateExecution();

        execution.Abort("Environnement instable, procédure interrompue");

        execution.Status.Should().Be(SopExecutionStatus.Aborted);
        execution.AbortReason.Should().Be("Environnement instable, procédure interrompue");
    }
}
