using FluentAssertions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Infrastructure.Persistence.Repositories;
using N4Sentinel.IntegrationTests.Fixtures;
using Xunit;

namespace N4Sentinel.IntegrationTests.Persistence;

public class EfEnvironmentRepositoryTests(SqlServerContainerFixture fixture) : IClassFixture<SqlServerContainerFixture>
{
    [SkippableFact]
    public async Task Add_ThenGetById_ReturnsPersistedEnvironment()
    {
        Skip.IfNot(fixture.IsAvailable, "Docker n'est pas disponible sur cette machine : test d'intégration ignoré.");

        await using var dbContext = fixture.CreateDbContext();
        var repository = new EfEnvironmentRepository(dbContext);
        var environment = new N4Environment("Production", "PROD-IT", EnvironmentKind.Production, "Test intégration");

        repository.Add(environment);
        await dbContext.SaveChangesAsync();

        await using var readContext = fixture.CreateDbContext();
        var readRepository = new EfEnvironmentRepository(readContext);
        var reloaded = await readRepository.GetByIdAsync(environment.Id, CancellationToken.None);

        reloaded.Should().NotBeNull();
        reloaded!.Name.Should().Be("Production");
        reloaded.Code.Should().Be("PROD-IT");
    }

    [SkippableFact]
    public async Task ExistsWithCode_AfterAdd_ReturnsTrue()
    {
        Skip.IfNot(fixture.IsAvailable, "Docker n'est pas disponible sur cette machine : test d'intégration ignoré.");

        await using var dbContext = fixture.CreateDbContext();
        var repository = new EfEnvironmentRepository(dbContext);
        var environment = new N4Environment("UAT", "UAT-IT", EnvironmentKind.Uat, null);
        repository.Add(environment);
        await dbContext.SaveChangesAsync();

        var exists = await repository.ExistsWithCodeAsync("uat-it", CancellationToken.None);

        exists.Should().BeTrue();
    }
}
