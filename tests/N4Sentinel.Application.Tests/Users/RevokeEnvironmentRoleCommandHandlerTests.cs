using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Users.Commands;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Users;

public class RevokeEnvironmentRoleCommandHandlerTests
{
    private readonly IUserEnvironmentRoleRepository roles = Substitute.For<IUserEnvironmentRoleRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private RevokeEnvironmentRoleCommandHandler CreateHandler() => new(roles, unitOfWork);

    [Fact]
    public async Task Handle_UnknownRole_ThrowsKeyNotFoundException()
    {
        roles.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((UserEnvironmentRole?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(new RevokeEnvironmentRoleCommand(Guid.NewGuid(), "admin1"), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_KnownRole_RemovesAndSaves()
    {
        var role = new UserEnvironmentRole("operateur1", Guid.NewGuid(), "Operateur", "admin1");
        roles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        var handler = CreateHandler();

        await handler.Handle(new RevokeEnvironmentRoleCommand(role.Id, "admin1"), CancellationToken.None);

        roles.Received(1).Remove(role);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
