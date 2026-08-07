using FluentAssertions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using Xunit;

namespace N4Sentinel.Domain.Tests.Entities;

public class UserEnvironmentRoleTests
{
    [Fact]
    public void Constructor_WithEmptyUserId_Throws()
    {
        var act = () => new UserEnvironmentRole("", Guid.NewGuid(), "Operateur", "admin1");

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Constructor_WithEmptyRole_Throws()
    {
        var act = () => new UserEnvironmentRole("user1", Guid.NewGuid(), "", "admin1");

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Constructor_WithEmptyGrantedBy_Throws()
    {
        var act = () => new UserEnvironmentRole("user1", Guid.NewGuid(), "Operateur", "");

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Constructor_WithValidData_Succeeds()
    {
        var environmentId = Guid.NewGuid();

        var role = new UserEnvironmentRole("user1", environmentId, "Operateur", "admin1");

        role.UserId.Should().Be("user1");
        role.EnvironmentId.Should().Be(environmentId);
        role.Role.Should().Be("Operateur");
        role.GrantedByUserId.Should().Be("admin1");
    }
}
