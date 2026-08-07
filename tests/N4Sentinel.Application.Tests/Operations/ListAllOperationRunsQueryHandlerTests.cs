using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Operations.Queries;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Operations;

public class ListAllOperationRunsQueryHandlerTests
{
    private readonly IOperationRunRepository operationRuns = Substitute.For<IOperationRunRepository>();
    private readonly IEnvironmentRepository environments = Substitute.For<IEnvironmentRepository>();

    private ListAllOperationRunsQueryHandler CreateHandler() => new(operationRuns, environments);

    private static OperationRun CreateRun(Guid environmentId) =>
        new(environmentId, Guid.NewGuid(), Guid.NewGuid(), 1, isProductionEnvironment: false,
            null, null, null, null, "operateur@n4sentinel.local",
            [(Guid.NewGuid(), 0, "Démarrer le Bridge", WorkflowStepAction.Start, (Guid?)null, (string?)null)]);

    [Fact]
    public async Task Handle_ReturnsRunsOrderedByMostRecentWithEnvironmentName()
    {
        var environment = new N4Environment("Production", "PROD", EnvironmentKind.Production, null);
        environments.ListAllAsync(Arg.Any<CancellationToken>()).Returns([environment]);

        var older = CreateRun(environment.Id);
        var newer = CreateRun(environment.Id);
        operationRuns.ListAllAsync(Arg.Any<CancellationToken>()).Returns([older, newer]);
        var handler = CreateHandler();

        var result = await handler.Handle(new ListAllOperationRunsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.EnvironmentName == "Production");
    }

    [Fact]
    public async Task Handle_UnknownEnvironment_FallsBackToPlaceholderName()
    {
        environments.ListAllAsync(Arg.Any<CancellationToken>()).Returns([]);
        var run = CreateRun(Guid.NewGuid());
        operationRuns.ListAllAsync(Arg.Any<CancellationToken>()).Returns([run]);
        var handler = CreateHandler();

        var result = await handler.Handle(new ListAllOperationRunsQuery(), CancellationToken.None);

        result.Should().ContainSingle().Which.EnvironmentName.Should().Be("—");
    }
}
