using FluentAssertions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using N4Sentinel.Domain.Services;
using Xunit;

namespace N4Sentinel.Domain.Tests.Services;

public class CenterRoleMaintenancePlannerTests
{
    private static readonly Guid EnvironmentId = Guid.NewGuid();

    private static N4Component Component(
        string name, N4ComponentKind kind,
        ComponentGovernance governance = ComponentGovernance.Controllable) =>
        new(EnvironmentId, name, "Center", ComponentCriticality.Critical, governance, kind: kind);

    private static N4Component[] Pair() =>
    [
        Component("CENTER", N4ComponentKind.CenterNode),
        Component("STANDBY", N4ComponentKind.StandbyCenterNode),
    ];

    // -----------------------------------------------------------------------------------------------------
    // FR-046 — continuité du rôle sur le primaire
    // -----------------------------------------------------------------------------------------------------

    [Fact]
    public void KeepRole_StopsTheStandbyBeforeThePrimary()
    {
        var plan = CenterRoleMaintenancePlanner.Plan(Pair(), CenterRoleStrategy.KeepRoleOnPrimary);

        var stops = plan.Steps.Where(s => s.Action == WorkflowStepAction.Stop).ToList();

        // C'est la règle qui protège d'une bascule non voulue : le Standby prendrait sinon le verrou.
        stops.Select(s => s.ComponentName).Should().ContainInOrder("STANDBY", "CENTER");
    }

    [Fact]
    public void KeepRole_RestartsTheStandbyOnlyAfterThePrimaryIsActive()
    {
        var plan = CenterRoleMaintenancePlanner.Plan(Pair(), CenterRoleStrategy.KeepRoleOnPrimary);

        var primaryStart = plan.Steps.Single(
            s => s.Action == WorkflowStepAction.Start && s.ComponentName == "CENTER").Position;
        var roleConfirmation = plan.Steps.Single(
            s => s.IsVerification && s.Label.Contains("repris le rôle")).Position;
        var standbyStart = plan.Steps.Single(
            s => s.Action == WorkflowStepAction.Start && s.ComponentName == "STANDBY").Position;

        primaryStart.Should().BeLessThan(roleConfirmation);
        roleConfirmation.Should().BeLessThan(
            standbyStart, "GUIDE §1.10.4 : « once the Center node is active, then start N4 on the Standby node »");
    }

    [Fact]
    public void KeepRole_WithoutStandby_PlansOnlyThePrimaryAndWarns()
    {
        var plan = CenterRoleMaintenancePlanner.Plan(
            [Component("CENTER", N4ComponentKind.CenterNode)], CenterRoleStrategy.KeepRoleOnPrimary);

        plan.Steps.Should().NotContain(s => s.ComponentName == "STANDBY");
        plan.Warnings.Should().Contain(w => w.Contains("Aucun Standby"));
    }

    // -----------------------------------------------------------------------------------------------------
    // FR-047 — bascule assumée
    // -----------------------------------------------------------------------------------------------------

    [Fact]
    public void Failover_VerifiesTheStandbyIsFitBeforeStoppingThePrimary()
    {
        var plan = CenterRoleMaintenancePlanner.Plan(Pair(), CenterRoleStrategy.AcceptFailover);

        var fitness = plan.Steps.Single(s => s.Label.Contains("apte à prendre le rôle")).Position;
        var primaryStop = plan.Steps.Single(s => s.Action == WorkflowStepAction.Stop).Position;

        fitness.Should().BeLessThan(
            primaryStop, "basculer vers un Standby inapte laisserait l'environnement sans Center actif");
    }

    [Fact]
    public void Failover_NeverStartsThePrimaryBackAutomatically()
    {
        var plan = CenterRoleMaintenancePlanner.Plan(Pair(), CenterRoleStrategy.AcceptFailover);

        // Relancer le primaire sans contrôle est exactement le scénario « deux Center actifs » interdit.
        plan.Steps.Should().NotContain(
            s => s.Action == WorkflowStepAction.Start && s.ComponentName == "CENTER");
        plan.Warnings.Should().Contain(w => w.Contains("deux Center actifs"));
    }

    [Fact]
    public void Failover_WithoutStandby_Throws()
    {
        var act = () => CenterRoleMaintenancePlanner.Plan(
            [Component("CENTER", N4ComponentKind.CenterNode)], CenterRoleStrategy.AcceptFailover);

        act.Should().Throw<DomainRuleException>().WithMessage("*aucune instance vers laquelle basculer*");
    }

    // -----------------------------------------------------------------------------------------------------
    // Invariant commun aux deux stratégies
    // -----------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(CenterRoleStrategy.KeepRoleOnPrimary)]
    [InlineData(CenterRoleStrategy.AcceptFailover)]
    public void EveryPlan_EndsWithASingleActiveRoleCheck(CenterRoleStrategy strategy)
    {
        var plan = CenterRoleMaintenancePlanner.Plan(Pair(), strategy);

        var last = plan.Steps.Last();
        last.IsVerification.Should().BeTrue();
        last.SuccessCriteria.Should().Contain("Exactement une instance Center", "FR-047");
    }

    [Theory]
    [InlineData(CenterRoleStrategy.KeepRoleOnPrimary)]
    [InlineData(CenterRoleStrategy.AcceptFailover)]
    public void EveryPlan_NumbersStepsContiguouslyFromOne(CenterRoleStrategy strategy)
    {
        var plan = CenterRoleMaintenancePlanner.Plan(Pair(), strategy);

        plan.Steps.Select(s => s.Position).Should().Equal(Enumerable.Range(1, plan.Steps.Count));
    }

    // -----------------------------------------------------------------------------------------------------
    // Cohérence du référentiel
    // -----------------------------------------------------------------------------------------------------

    [Fact]
    public void Plan_WithoutAnyCenterNode_Throws()
    {
        var act = () => CenterRoleMaintenancePlanner.Plan(
            [Component("CLUSTER-01", N4ComponentKind.ClusterNode)], CenterRoleStrategy.KeepRoleOnPrimary);

        act.Should().Throw<DomainRuleException>().WithMessage("*Aucun Center Node*");
    }

    [Fact]
    public void Plan_WithTwoCenterNodes_Throws()
    {
        var components = new[]
        {
            Component("CENTER-A", N4ComponentKind.CenterNode),
            Component("CENTER-B", N4ComponentKind.CenterNode),
        };

        var act = () => CenterRoleMaintenancePlanner.Plan(components, CenterRoleStrategy.KeepRoleOnPrimary);

        act.Should().Throw<DomainRuleException>().WithMessage("*qu'un Center primaire*");
    }

    [Fact]
    public void Plan_WithNonControllableCenter_Throws()
    {
        var components = new[]
        {
            Component("CENTER", N4ComponentKind.CenterNode, ComponentGovernance.SupervisedOnly),
        };

        var act = () => CenterRoleMaintenancePlanner.Plan(components, CenterRoleStrategy.KeepRoleOnPrimary);

        act.Should().Throw<DomainRuleException>().WithMessage("*pilotable*");
    }
}
