using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Users.Commands;
using N4Sentinel.Domain.Exceptions;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Users;

public class LockUserAccountCommandHandlerTests
{
    private readonly IUserRoleService userRoles = Substitute.For<IUserRoleService>();

    private LockUserAccountCommandHandler CreateHandler() => new(userRoles);

    [Fact]
    public async Task Handle_SelfLock_Throws()
    {
        var handler = CreateHandler();

        var act = () => handler.Handle(new LockUserAccountCommand("user-1", "user-1"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainRuleException>();
        await userRoles.DidNotReceiveWithAnyArgs().LockAsync(default!, default);
    }

    [Fact]
    public async Task Handle_LockingAnotherUser_Succeeds()
    {
        var handler = CreateHandler();

        await handler.Handle(new LockUserAccountCommand("user-1", "user-2"), CancellationToken.None);

        await userRoles.Received(1).LockAsync("user-1", Arg.Any<CancellationToken>());
    }
}
