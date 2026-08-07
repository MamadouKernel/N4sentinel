using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Reconstitution.Commands;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Reconstitution;

public class ConfirmReconstitutionStepCommandHandlerTests
{
    private readonly IFolderReconstitutionRepository reconstitutions = Substitute.For<IFolderReconstitutionRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private ConfirmReconstitutionStepCommandHandler CreateHandler() => new(reconstitutions, unitOfWork);

    [Fact]
    public async Task Handle_UnknownReconstitution_ThrowsKeyNotFoundException()
    {
        reconstitutions.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((FolderReconstitution?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new ConfirmReconstitutionStepCommand(Guid.NewGuid(), "operateur@n4sentinel.local", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_KnownReconstitution_ConfirmsNextStepAndSaves()
    {
        var reconstitution = new FolderReconstitution(Guid.NewGuid(), "Suspicion de corruption", "operateur@n4sentinel.local");
        reconstitutions.GetByIdAsync(reconstitution.Id, Arg.Any<CancellationToken>()).Returns(reconstitution);
        var handler = CreateHandler();

        await handler.Handle(
            new ConfirmReconstitutionStepCommand(reconstitution.Id, "operateur@n4sentinel.local", "Cluster arrêté"),
            CancellationToken.None);

        reconstitution.Steps.Should().ContainSingle().Which.Step.Should().Be(ReconstitutionStepKind.StopComponents);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
