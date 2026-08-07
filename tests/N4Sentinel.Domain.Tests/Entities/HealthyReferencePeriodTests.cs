using FluentAssertions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using Xunit;

namespace N4Sentinel.Domain.Tests.Entities;

public class HealthyReferencePeriodTests
{
    [Fact]
    public void Constructor_WithEmptyLabel_Throws()
    {
        var act = () => new HealthyReferencePeriod(
            Guid.NewGuid(), "", DateTime.UtcNow.AddDays(-7), DateTime.UtcNow.AddDays(-6), null, "admin1");

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Constructor_EndBeforeStart_Throws()
    {
        var act = () => new HealthyReferencePeriod(
            Guid.NewGuid(), "Semaine calme", DateTime.UtcNow, DateTime.UtcNow.AddDays(-1), null, "admin1");

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Constructor_WithEmptyValidator_Throws()
    {
        var act = () => new HealthyReferencePeriod(
            Guid.NewGuid(), "Semaine calme", DateTime.UtcNow.AddDays(-7), DateTime.UtcNow.AddDays(-6), null, "");

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Constructor_WithValidData_Succeeds()
    {
        var start = DateTime.UtcNow.AddDays(-14);
        var end = DateTime.UtcNow.AddDays(-7);

        var period = new HealthyReferencePeriod(Guid.NewGuid(), "Semaine calme", start, end, "Aucun incident signalé", "admin1");

        period.Label.Should().Be("Semaine calme");
        period.PeriodStartUtc.Should().Be(start);
        period.PeriodEndUtc.Should().Be(end);
        period.ValidatedByUserId.Should().Be("admin1");
    }
}
