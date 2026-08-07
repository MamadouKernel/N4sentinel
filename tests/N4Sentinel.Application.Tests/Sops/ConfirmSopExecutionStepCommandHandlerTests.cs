using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Sops.Commands;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Sops;

public class ConfirmSopExecutionStepCommandHandlerTests
{
    private readonly ISopExecutionRepository executions = Substitute.For<ISopExecutionRepository>();
    private readonly ISopRepository sops = Substitute.For<ISopRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private ConfirmSopExecutionStepCommandHandler CreateHandler() => new(executions, sops, unitOfWork);

    private static Sop CreateActiveSop()
    {
        var sop = new Sop(
            "SOP-X", "Titre", "Objectif", null, "Arrêter le service\nVérifier les logs", null, null, null, null);
        sop.SubmitForValidation();
        sop.Validate();
        sop.Activate();
        return sop;
    }

    [Fact]
    public async Task Handle_ConfirmsStepUsingTextFromSop()
    {
        var sop = CreateActiveSop();
        var execution = new SopExecution(sop.Id, sop.VersionNumber, "operateur1");
        executions.GetByIdAsync(execution.Id, Arg.Any<CancellationToken>()).Returns(execution);
        sops.GetByIdAsync(sop.Id, Arg.Any<CancellationToken>()).Returns(sop);
        var handler = CreateHandler();

        await handler.Handle(
            new ConfirmSopExecutionStepCommand(execution.Id, "operateur1", "capture.png", null), CancellationToken.None);

        execution.StepConfirmations.Should().ContainSingle().Which.StepText.Should().Be("Arrêter le service");
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AllStepsAlreadyConfirmed_ThrowsDomainRuleException()
    {
        var sop = CreateActiveSop();
        var execution = new SopExecution(sop.Id, sop.VersionNumber, "operateur1");
        execution.ConfirmNextStep("Arrêter le service", "operateur1", null, null);
        execution.ConfirmNextStep("Vérifier les logs", "operateur1", null, null);
        executions.GetByIdAsync(execution.Id, Arg.Any<CancellationToken>()).Returns(execution);
        sops.GetByIdAsync(sop.Id, Arg.Any<CancellationToken>()).Returns(sop);
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new ConfirmSopExecutionStepCommand(execution.Id, "operateur1", null, null), CancellationToken.None);

        await act.Should().ThrowAsync<DomainRuleException>();
    }
}
