using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Sops.Commands;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Sops;

public class ActivateSopCommandHandlerTests
{
    private readonly ISopRepository sops = Substitute.For<ISopRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private ActivateSopCommandHandler CreateHandler() => new(sops, unitOfWork);

    private static Sop CreateValidatedSop()
    {
        var sop = new Sop("SOP-X", "Titre", "Objectif", null, "Étape 1", null, null, null, null);
        sop.SubmitForValidation();
        sop.Validate();
        return sop;
    }

    [Fact]
    public async Task Handle_ActivatesSop_AndDisablesPreviousActiveSibling()
    {
        var previousActive = CreateValidatedSop();
        previousActive.Activate();

        var newVersion = new Sop("SOP-X", "Titre v2", "Objectif", null, "Étape 1", null, null, null, null);
        newVersion.SubmitForValidation();
        newVersion.Validate();

        sops.GetByIdAsync(newVersion.Id, Arg.Any<CancellationToken>()).Returns(newVersion);
        sops.ListBySopKeyAsync("SOP-X", Arg.Any<CancellationToken>()).Returns([previousActive, newVersion]);
        var handler = CreateHandler();

        await handler.Handle(new ActivateSopCommand(newVersion.Id), CancellationToken.None);

        newVersion.Status.Should().Be(SopStatus.Active);
        previousActive.Status.Should().Be(SopStatus.Disabled);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownSopId_ThrowsKeyNotFoundException()
    {
        sops.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Sop?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(new ActivateSopCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
