using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Components.Commands;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Components;

/// <summary>
/// Le typage d'un composant conditionne tout le séquencement d'arrêt/démarrage : un composant resté
/// <see cref="N4ComponentKind.Unspecified"/> n'apparaît dans aucune séquence. Ces tests verrouillent le
/// fait que le type traverse réellement la commande jusqu'à l'entité — l'oublier rendrait la fonctionnalité
/// silencieusement inerte.
/// </summary>
public class ComponentKindWiringTests
{
    private readonly IEnvironmentRepository environments = Substitute.For<IEnvironmentRepository>();
    private readonly IComponentRepository components = Substitute.For<IComponentRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private static readonly Guid EnvironmentId = Guid.NewGuid();

    [Fact]
    public async Task CreateComponent_PersistsTheChosenKind()
    {
        environments.GetByIdAsync(EnvironmentId, Arg.Any<CancellationToken>())
            .Returns(new N4Environment("Production", "PROD", EnvironmentKind.Production, null));

        N4Component? captured = null;
        components.When(r => r.Add(Arg.Any<N4Component>())).Do(ci => captured = ci.Arg<N4Component>());

        var handler = new CreateComponentCommandHandler(environments, components, unitOfWork);

        await handler.Handle(
            new CreateComponentCommand(
                EnvironmentId, "CLUSTER-01", "Nœud applicatif", null, null, null, null, null, null,
                ComponentCriticality.Critical, ComponentGovernance.Controllable, null, null, [], "admin",
                N4ComponentKind.ClusterNode),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Kind.Should().Be(N4ComponentKind.ClusterNode);
    }

    [Fact]
    public async Task UpdateComponent_ChangesTheKind()
    {
        var component = new N4Component(
            EnvironmentId, "CENTER", "Center", ComponentCriticality.Critical, ComponentGovernance.Controllable);
        component.Kind.Should().Be(N4ComponentKind.Unspecified, "un composant existant n'est pas typé par défaut");

        components.GetByIdAsync(component.Id, Arg.Any<CancellationToken>()).Returns(component);

        var handler = new UpdateComponentCommandHandler(components, unitOfWork);

        await handler.Handle(
            new UpdateComponentCommand(
                component.Id, "CENTER", "Center", null, null, null, null, null, null,
                ComponentCriticality.Critical, ComponentGovernance.Controllable, null, null, [], "admin",
                N4ComponentKind.CenterNode),
            CancellationToken.None);

        component.Kind.Should().Be(N4ComponentKind.CenterNode);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateComponent_WithoutKind_DefaultsToUnspecified()
    {
        environments.GetByIdAsync(EnvironmentId, Arg.Any<CancellationToken>())
            .Returns(new N4Environment("UAT", "UAT", EnvironmentKind.Uat, null));

        N4Component? captured = null;
        components.When(r => r.Add(Arg.Any<N4Component>())).Do(ci => captured = ci.Arg<N4Component>());

        var handler = new CreateComponentCommandHandler(environments, components, unitOfWork);

        await handler.Handle(
            new CreateComponentCommand(
                EnvironmentId, "DIVERS", "Autre", null, null, null, null, null, null,
                ComponentCriticality.Low, ComponentGovernance.SupervisedOnly, null, null, [], "admin"),
            CancellationToken.None);

        captured!.Kind.Should().Be(N4ComponentKind.Unspecified);
    }
}
