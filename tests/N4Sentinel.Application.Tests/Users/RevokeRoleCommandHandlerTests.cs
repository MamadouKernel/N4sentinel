using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Users;
using N4Sentinel.Application.Users.Commands;
using N4Sentinel.Domain.Exceptions;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Users;

public class RevokeRoleCommandHandlerTests
{
    private readonly IUserRoleService userRoles = Substitute.For<IUserRoleService>();

    private RevokeRoleCommandHandler CreateHandler() => new(userRoles);

    [Fact]
    public async Task Handle_SelfRevokingAdministrateur_Throws()
    {
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new RevokeRoleCommand("user-1", Roles.Administrateur, "user-1"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainRuleException>();
        await userRoles.DidNotReceiveWithAnyArgs().RevokeRoleAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_SelfRevokingOtherRole_Succeeds()
    {
        var handler = CreateHandler();

        await handler.Handle(new RevokeRoleCommand("user-1", Roles.Lecteur, "user-1"), CancellationToken.None);

        await userRoles.Received(1).RevokeRoleAsync("user-1", Roles.Lecteur, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RevokingAnotherUsersAdministrateur_Succeeds()
    {
        var handler = CreateHandler();

        await handler.Handle(new RevokeRoleCommand("user-1", Roles.Administrateur, "user-2"), CancellationToken.None);

        await userRoles.Received(1).RevokeRoleAsync("user-1", Roles.Administrateur, Arg.Any<CancellationToken>());
    }
}
