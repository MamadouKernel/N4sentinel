using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Diagnostics.Commands;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Diagnostics;

public class CreateHealthyReferencePeriodCommandHandlerTests
{
    private readonly IHealthyReferencePeriodRepository periods = Substitute.For<IHealthyReferencePeriodRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private CreateHealthyReferencePeriodCommandHandler CreateHandler() => new(periods, unitOfWork);

    [Fact]
    public async Task Handle_ValidRequest_CreatesPeriodAndSaves()
    {
        var environmentId = Guid.NewGuid();
        var start = DateTime.UtcNow.AddDays(-14);
        var end = DateTime.UtcNow.AddDays(-7);
        var handler = CreateHandler();

        var id = await handler.Handle(
            new CreateHealthyReferencePeriodCommand(environmentId, "Semaine calme", start, end, "RAS", "admin1"),
            CancellationToken.None);

        id.Should().NotBeEmpty();
        periods.Received(1).Add(Arg.Is<HealthyReferencePeriod>(p =>
            p!.Label == "Semaine calme" && p.EnvironmentId == environmentId && p.ValidatedByUserId == "admin1"));
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
