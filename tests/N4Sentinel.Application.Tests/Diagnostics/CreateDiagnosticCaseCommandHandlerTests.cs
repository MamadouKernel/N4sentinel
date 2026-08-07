using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Diagnostics.Commands;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Diagnostics;

public class CreateDiagnosticCaseCommandHandlerTests
{
    private readonly IEnvironmentRepository environments = Substitute.For<IEnvironmentRepository>();
    private readonly IDiagnosticCaseRepository cases = Substitute.For<IDiagnosticCaseRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private CreateDiagnosticCaseCommandHandler CreateHandler() => new(environments, cases, unitOfWork);

    [Fact]
    public async Task Handle_UnknownEnvironment_ThrowsKeyNotFoundException()
    {
        environments.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((N4Environment?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new CreateDiagnosticCaseCommand(
                Guid.NewGuid(), "Bridge indisponible", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, "INC-1", "operateur@n4sentinel.local"),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_KnownEnvironment_CreatesCaseAndSaves()
    {
        var environment = new N4Environment("Production", "PROD", EnvironmentKind.Production, null);
        environments.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);
        var handler = CreateHandler();

        var id = await handler.Handle(
            new CreateDiagnosticCaseCommand(
                environment.Id, "Bridge indisponible", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, "INC-1", "operateur@n4sentinel.local"),
            CancellationToken.None);

        id.Should().NotBeEmpty();
        cases.Received(1).Add(Arg.Is<DiagnosticCase>(c => c!.Symptom == "Bridge indisponible" && c.EnvironmentId == environment.Id));
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
