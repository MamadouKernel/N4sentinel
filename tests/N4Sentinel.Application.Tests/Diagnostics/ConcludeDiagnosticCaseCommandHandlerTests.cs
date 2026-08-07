using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Diagnostics.Commands;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Diagnostics;

public class ConcludeDiagnosticCaseCommandHandlerTests
{
    private readonly IDiagnosticCaseRepository cases = Substitute.For<IDiagnosticCaseRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private ConcludeDiagnosticCaseCommandHandler CreateHandler() => new(cases, unitOfWork);

    [Fact]
    public async Task Handle_UnknownCase_ThrowsKeyNotFoundException()
    {
        cases.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DiagnosticCase?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new ConcludeDiagnosticCaseCommand(Guid.NewGuid(), ConclusionLevel.NoAnomalyDetected, "Périmètre : réseau."),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_KnownCase_ConcludesAndSaves()
    {
        var diagnosticCase = new DiagnosticCase(
            Guid.NewGuid(), "Bridge indisponible", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, "INC-1", "operateur@n4sentinel.local");
        cases.GetByIdAsync(diagnosticCase.Id, Arg.Any<CancellationToken>()).Returns(diagnosticCase);
        var handler = CreateHandler();

        await handler.Handle(
            new ConcludeDiagnosticCaseCommand(diagnosticCase.Id, ConclusionLevel.VeryLikelyCause, "Analyse limitée au réseau."),
            CancellationToken.None);

        diagnosticCase.ConclusionLevel.Should().Be(ConclusionLevel.VeryLikelyCause);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
