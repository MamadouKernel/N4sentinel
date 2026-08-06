using Microsoft.EntityFrameworkCore;
using N4Sentinel.Infrastructure.Persistence;
using Testcontainers.MsSql;
using Xunit;

namespace N4Sentinel.IntegrationTests.Fixtures;

/// <summary>
/// Démarre un vrai SQL Server dans un conteneur Docker pour les tests d'intégration des repositories EF Core.
/// Si Docker n'est pas disponible sur la machine (cas de ce poste de développement), <see cref="IsAvailable"/>
/// reste à false et les tests utilisant cette fixture sont marqués "skip" plutôt qu'échoués. La construction
/// du conteneur elle-même (pas seulement son démarrage) peut échouer si le daemon Docker est injoignable,
/// d'où le try/catch englobant aussi <c>new MsSqlBuilder().Build()</c>.
/// </summary>
public sealed class SqlServerContainerFixture : IAsyncLifetime
{
    private MsSqlContainer? container;

    public bool IsAvailable { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
            await container.StartAsync();

            await using var dbContext = CreateDbContext();
            await dbContext.Database.MigrateAsync();

            IsAvailable = true;
        }
        catch
        {
            IsAvailable = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (container is not null)
        {
            await container.DisposeAsync();
        }
    }

    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(container!.GetConnectionString())
            .Options;

        return new AppDbContext(options);
    }
}
