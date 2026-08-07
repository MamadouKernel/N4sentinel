using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Environments.Commands;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Environments;

public class ChangeEnvironmentStatusCommandHandlerTests
{
    private readonly IEnvironmentRepository environments = Substitute.For<IEnvironmentRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private ChangeEnvironmentStatusCommandHandler CreateHandler() => new(environments, unitOfWork);

    [Fact]
    public async Task Handle_UnknownEnvironment_ThrowsKeyNotFoundException()
    {
        environments.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((N4Environment?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new ChangeEnvironmentStatusCommand(Guid.NewGuid(), EnvironmentStatusAction.SubmitForValidation, "admin1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_SubmitForValidation_ChangesStatusAndSaves()
    {
        var environment = new N4Environment("Production", "PROD", EnvironmentKind.Production, null);
        environments.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);
        var handler = CreateHandler();

        await handler.Handle(
            new ChangeEnvironmentStatusCommand(environment.Id, EnvironmentStatusAction.SubmitForValidation, "admin1"),
            CancellationToken.None);

        environment.Status.Should().Be(EnvironmentStatus.PendingValidation);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidTransition_ThrowsDomainRuleExceptionAndDoesNotSave()
    {
        var environment = new N4Environment("Production", "PROD", EnvironmentKind.Production, null);
        environments.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new ChangeEnvironmentStatusCommand(environment.Id, EnvironmentStatusAction.Activate, "admin1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainRuleException>();
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
