using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Workflows.Commands;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Workflows;

public class AddWorkflowStepCommandHandlerTests
{
    private readonly IWorkflowRepository workflows = Substitute.For<IWorkflowRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private AddWorkflowStepCommandHandler CreateHandler() => new(workflows, unitOfWork);

    private static Workflow CreateWorkflow() =>
        new(Guid.NewGuid(), "Démarrage complet", WorkflowType.Start, WorkflowScope.Full, []);

    [Fact]
    public async Task Handle_UnknownWorkflow_ThrowsKeyNotFoundException()
    {
        workflows.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Workflow?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new AddWorkflowStepCommand(
                Guid.NewGuid(), Guid.NewGuid(), "Étape", null, WorkflowStepAction.Start, [], null, null, null,
                null, 0, false, false, null, WorkflowStepFailurePolicy.StopWorkflow, false, false, false, "admin1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_KnownDraftVersion_AddsStepAndSaves()
    {
        var workflow = CreateWorkflow();
        var draftVersionId = workflow.LatestVersion.Id;
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        var handler = CreateHandler();

        var stepId = await handler.Handle(
            new AddWorkflowStepCommand(
                workflow.Id, draftVersionId, "Démarrer le Bridge", null, WorkflowStepAction.Start, [], null, 30,
                60, 120, 0, false, false, null, WorkflowStepFailurePolicy.StopWorkflow, true, false, false, "admin1"),
            CancellationToken.None);

        stepId.Should().NotBeEmpty();
        workflow.LatestVersion.Steps.Should().ContainSingle(s => s.Name == "Démarrer le Bridge");
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
