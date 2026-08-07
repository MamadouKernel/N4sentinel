using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Users.Queries;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Users;

public class HasEnvironmentRoleQueryHandlerTests
{
    private readonly IUserEnvironmentRoleRepository roles = Substitute.For<IUserEnvironmentRoleRepository>();

    private HasEnvironmentRoleQueryHandler CreateHandler() => new(roles);

    [Fact]
    public async Task Handle_DelegatesToRepository()
    {
        var environmentId = Guid.NewGuid();
        var requestedRoles = new[] { "Operateur", "Administrateur" };
        roles.HasAnyRoleForEnvironmentAsync("user1", environmentId, requestedRoles, Arg.Any<CancellationToken>()).Returns(true);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new HasEnvironmentRoleQuery("user1", environmentId, requestedRoles), CancellationToken.None);

        result.Should().BeTrue();
    }
}
