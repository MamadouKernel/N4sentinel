using FluentAssertions;
using N4Sentinel.Application.Common;
using N4Sentinel.Application.Components.Commands;
using N4Sentinel.Application.Environments.Commands;
using N4Sentinel.Application.Users.Commands;
using N4Sentinel.Application.Workflows.Commands;
using N4Sentinel.Domain.Entities;
using Xunit;

namespace N4Sentinel.Application.Tests.Common;

/// <summary>
/// Constat d'audit sécurité (2026-08-07, Sprint 16) : E10.1 promettait un "journal d'audit complet" mais
/// n'auditait ni le référentiel (Environnements/Composants/Workflows) ni le verrouillage/déverrouillage de
/// compte. Ces tests vérifient que les commandes de mutation du référentiel et de gestion de compte portent
/// désormais <see cref="IAuditableRequest"/> avec un identifiant d'acteur correctement relayé.
/// </summary>
public class ReferentialAuditCoverageTests
{
    [Fact]
    public void CreateEnvironmentCommand_IsAuditable()
    {
        var command = new CreateEnvironmentCommand("Production", "PROD", EnvironmentKind.Production, null, "admin1");

        var auditable = command.Should().BeAssignableTo<IAuditableRequest>().Subject;
        auditable.ActorUserId.Should().Be("admin1");
        auditable.Action.Should().NotBeNullOrWhiteSpace();
        auditable.Summary.Should().Contain("PROD");
    }

    [Fact]
    public void CreateComponentCommand_IsAuditable()
    {
        var command = new CreateComponentCommand(
            Guid.NewGuid(), "Bridge", "Bridge daemon", null, null, null, null, null, null,
            ComponentCriticality.High, ComponentGovernance.Controllable, null, null, [], "operateur1");

        var auditable = command.Should().BeAssignableTo<IAuditableRequest>().Subject;
        auditable.ActorUserId.Should().Be("operateur1");
    }

    [Fact]
    public void CreateWorkflowCommand_IsAuditable()
    {
        var command = new CreateWorkflowCommand(
            Guid.NewGuid(), "Démarrage complet", WorkflowType.Start, WorkflowScope.Full, [], "admin1");

        var auditable = command.Should().BeAssignableTo<IAuditableRequest>().Subject;
        auditable.ActorUserId.Should().Be("admin1");
    }

    [Fact]
    public void LockUserAccountCommand_IsAuditable()
    {
        var command = new LockUserAccountCommand("user-42", "admin1");

        var auditable = command.Should().BeAssignableTo<IAuditableRequest>().Subject;
        auditable.ActorUserId.Should().Be("admin1");
        auditable.Summary.Should().Contain("user-42");
    }

    [Fact]
    public void UnlockUserAccountCommand_IsAuditable()
    {
        var command = new UnlockUserAccountCommand("user-42", "admin1");

        var auditable = command.Should().BeAssignableTo<IAuditableRequest>().Subject;
        auditable.ActorUserId.Should().Be("admin1");
        auditable.Summary.Should().Contain("user-42");
    }
}
