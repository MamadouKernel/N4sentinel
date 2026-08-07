using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Users.Commands;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Users;

public class GrantEnvironmentRoleCommandHandlerTests
{
    private readonly IUserEnvironmentRoleRepository roles = Substitute.For<IUserEnvironmentRoleRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private GrantEnvironmentRoleCommandHandler CreateHandler() => new(roles, unitOfWork);

    [Fact]
    public async Task Handle_ValidRequest_GrantsRoleAndSaves()
    {
        var environmentId = Guid.NewGuid();
        var handler = CreateHandler();

        var id = await handler.Handle(
            new GrantEnvironmentRoleCommand("operateur1", environmentId, "Operateur", "admin1"), CancellationToken.None);

        id.Should().NotBeEmpty();
        roles.Received(1).Add(Arg.Is<UserEnvironmentRole>(r =>
            r!.UserId == "operateur1" && r.EnvironmentId == environmentId && r.Role == "Operateur"));
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class GrantEnvironmentRoleCommandValidatorTests
{
    [Theory]
    [InlineData("Lecteur", true)]
    [InlineData("Operateur", true)]
    [InlineData("Approbateur", true)]
    [InlineData("Administrateur", true)]
    [InlineData("SuperAdmin", false)]
    public void Validate_RoleMustBeOneOfTheFourKnownRoles(string role, bool expectedValid)
    {
        var validator = new GrantEnvironmentRoleCommandValidator();
        var command = new GrantEnvironmentRoleCommand("user1", Guid.NewGuid(), role, "admin1");

        var result = validator.Validate(command);

        result.IsValid.Should().Be(expectedValid);
    }
}
