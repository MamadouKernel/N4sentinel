using FluentAssertions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using Xunit;

namespace N4Sentinel.Domain.Tests.Entities;

public class FolderReconstitutionTests
{
    private static FolderReconstitution CreateReconstitution() =>
        new(Guid.NewGuid(), "Suspicion de corruption KahaDB", "operateur@n4sentinel.local");

    [Fact]
    public void Constructor_WithEmptyReason_Throws()
    {
        var act = () => new FolderReconstitution(Guid.NewGuid(), "", "operateur@n4sentinel.local");

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void NewReconstitution_IsInProgress_WithFirstStepStopComponents()
    {
        var reconstitution = CreateReconstitution();

        reconstitution.Status.Should().Be(ReconstitutionStatus.InProgress);
        reconstitution.NextStep.Should().Be(ReconstitutionStepKind.StopComponents);
        reconstitution.Steps.Should().BeEmpty();
    }

    [Fact]
    public void ConfirmNextStep_RecordsStepInFixedOrder()
    {
        var reconstitution = CreateReconstitution();

        reconstitution.ConfirmNextStep("operateur@n4sentinel.local", "Cluster arrêté");

        reconstitution.Steps.Should().ContainSingle().Which.Step.Should().Be(ReconstitutionStepKind.StopComponents);
        reconstitution.NextStep.Should().Be(ReconstitutionStepKind.Backup);
    }

    [Fact]
    public void ConfirmNextStep_AllSixSteps_CompletesReconstitution()
    {
        var reconstitution = CreateReconstitution();

        for (var i = 0; i < 6; i++)
        {
            reconstitution.ConfirmNextStep("operateur@n4sentinel.local", null);
        }

        reconstitution.Status.Should().Be(ReconstitutionStatus.Completed);
        reconstitution.NextStep.Should().BeNull();
        reconstitution.CompletedAtUtc.Should().NotBeNull();
        reconstitution.Steps.Select(s => s.Step).Should().Equal(
            ReconstitutionStepKind.StopComponents, ReconstitutionStepKind.Backup,
            ReconstitutionStepKind.VerifyBackupIntegrity, ReconstitutionStepKind.Reconstruct,
            ReconstitutionStepKind.ControlledRestart, ReconstitutionStepKind.FinalTests);
    }

    [Fact]
    public void ConfirmNextStep_AfterCompletion_Throws()
    {
        var reconstitution = CreateReconstitution();
        for (var i = 0; i < 6; i++)
        {
            reconstitution.ConfirmNextStep("operateur@n4sentinel.local", null);
        }

        var act = () => reconstitution.ConfirmNextStep("operateur@n4sentinel.local", null);

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Abort_WithoutReason_Throws()
    {
        var reconstitution = CreateReconstitution();

        var act = () => reconstitution.Abort("");

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Abort_SetsStatusAndReason()
    {
        var reconstitution = CreateReconstitution();
        reconstitution.ConfirmNextStep("operateur@n4sentinel.local", null);

        reconstitution.Abort("Cas incertain, escaladé à l'éditeur");

        reconstitution.Status.Should().Be(ReconstitutionStatus.Aborted);
        reconstitution.AbortReason.Should().Be("Cas incertain, escaladé à l'éditeur");
        reconstitution.Steps.Should().ContainSingle();
    }

    [Fact]
    public void Abort_AfterCompletion_Throws()
    {
        var reconstitution = CreateReconstitution();
        for (var i = 0; i < 6; i++)
        {
            reconstitution.ConfirmNextStep("operateur@n4sentinel.local", null);
        }

        var act = () => reconstitution.Abort("Trop tard");

        act.Should().Throw<DomainRuleException>();
    }
}
