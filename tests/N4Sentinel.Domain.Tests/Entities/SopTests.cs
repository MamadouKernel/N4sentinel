using FluentAssertions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using Xunit;

namespace N4Sentinel.Domain.Tests.Entities;

public class SopTests
{
    private static Sop CreateSop() => new(
        "SOP-CLUSTER-RESTART", "Redémarrage contrôlé d'un Cluster Node", "Rétablir un Cluster Node en échec",
        "Aucune opération critique en cours", "Arrêter le service\nVérifier les logs\nRedémarrer le service",
        "Le service répond sur le port 8080", "Perte de connexions actives", "Restaurer depuis la dernière sauvegarde",
        "3.8.25");

    [Fact]
    public void Constructor_WithEmptySopKey_Throws()
    {
        var act = () => new Sop("", "Titre", "Objectif", null, "Étape 1", null, null, null, null);

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Constructor_WithEmptySteps_Throws()
    {
        var act = () => new Sop("SOP-X", "Titre", "Objectif", null, "   ", null, null, null, null);

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void NewSop_IsDraftVersion1_NotReusable_NotGenerated()
    {
        var sop = CreateSop();

        sop.VersionNumber.Should().Be(1);
        sop.Status.Should().Be(SopStatus.Draft);
        sop.IsReusable.Should().BeFalse();
        sop.IsGeneratedFromExecution.Should().BeFalse();
    }

    [Fact]
    public void Steps_SplitsStepsTextByLine()
    {
        var sop = CreateSop();

        sop.Steps.Should().Equal("Arrêter le service", "Vérifier les logs", "Redémarrer le service");
    }

    [Fact]
    public void FullLifecycle_DraftToActive_IsReusable()
    {
        var sop = CreateSop();

        sop.SubmitForValidation();
        sop.Validate();
        sop.Activate();

        sop.Status.Should().Be(SopStatus.Active);
        sop.IsReusable.Should().BeTrue();
    }

    [Fact]
    public void Activate_WithoutValidation_Throws()
    {
        var sop = CreateSop();

        var act = () => sop.Activate();

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void UpdateContent_WhenNotDraft_Throws()
    {
        var sop = CreateSop();
        sop.SubmitForValidation();

        var act = () => sop.UpdateContent("Nouveau titre", "Nouvel objectif", null, "Étape 1", null, null, null, null);

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void CreateNewVersion_SharesSopKeyWithIncrementedVersionAsDraft()
    {
        var sop = CreateSop();
        sop.SubmitForValidation();
        sop.Validate();
        sop.Activate();

        var newVersion = sop.CreateNewVersion();

        newVersion.SopKey.Should().Be(sop.SopKey);
        newVersion.VersionNumber.Should().Be(2);
        newVersion.Status.Should().Be(SopStatus.Draft);
        sop.Status.Should().Be(SopStatus.Active, "creating a new version must not affect the original");
    }

    [Fact]
    public void Disable_FromDraft_Throws()
    {
        var sop = CreateSop();

        var act = () => sop.Disable();

        act.Should().Throw<DomainRuleException>();
    }
}
