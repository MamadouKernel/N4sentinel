using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Users;
using N4Sentinel.Application.Users.Commands;
using N4Sentinel.Domain.Exceptions;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Users;

public class GrantRoleCommandHandlerTests
{
    private readonly IUserRoleService userRoles = Substitute.For<IUserRoleService>();

    private GrantRoleCommandHandler CreateHandler() => new(userRoles);

    [Fact]
    public async Task Handle_SelfGrantingRole_Throws()
    {
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new GrantRoleCommand("user-1", Roles.Approbateur, "user-1"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainRuleException>();
        await userRoles.DidNotReceiveWithAnyArgs().GrantRoleAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_GrantingAnotherUsersRole_Succeeds()
    {
        var handler = CreateHandler();

        await handler.Handle(new GrantRoleCommand("user-1", Roles.Approbateur, "user-2"), CancellationToken.None);

        await userRoles.Received(1).GrantRoleAsync("user-1", Roles.Approbateur, Arg.Any<CancellationToken>());
    }
}
