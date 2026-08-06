using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Workflows.Commands;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Workflows;

public class CreateWorkflowCommandHandlerTests
{
    private readonly IEnvironmentRepository environments = Substitute.For<IEnvironmentRepository>();
    private readonly IWorkflowRepository workflows = Substitute.For<IWorkflowRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private CreateWorkflowCommandHandler CreateHandler() => new(environments, workflows, unitOfWork);

    [Fact]
    public async Task Handle_UnknownEnvironment_ThrowsKeyNotFoundException()
    {
        environments.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((N4Environment?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new CreateWorkflowCommand(Guid.NewGuid(), "Démarrage complet", WorkflowType.Start, WorkflowScope.Full, []),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_KnownEnvironment_CreatesWorkflowWithInitialDraftVersion()
    {
        var environment = new N4Environment("Production", "PROD", EnvironmentKind.Production, null);
        environments.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);
        var handler = CreateHandler();

        var id = await handler.Handle(
            new CreateWorkflowCommand(environment.Id, "Démarrage complet", WorkflowType.Start, WorkflowScope.Full, []),
            CancellationToken.None);

        id.Should().NotBeEmpty();
        workflows.Received(1).Add(Arg.Is<Workflow>(w =>
            w!.Name == "Démarrage complet" &&
            w.EnvironmentId == environment.Id &&
            w.Versions.Count == 1 &&
            w.LatestVersion.Status == WorkflowVersionStatus.Draft));
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
