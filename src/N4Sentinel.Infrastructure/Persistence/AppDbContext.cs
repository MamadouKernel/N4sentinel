using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence;

/// <summary>
/// Contexte EF Core du référentiel métier N4 Sentinel (environnements, composants...).
/// Distinct du contexte Identity (authentification/rôles), qui reste dans le projet Web et pointe sur
/// la même base de données : les deux contextes ont chacun leur propre historique de migrations.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<N4Environment> Environments => Set<N4Environment>();

    public DbSet<N4Component> Components => Set<N4Component>();

    public DbSet<Workflow> Workflows => Set<Workflow>();

    public DbSet<WorkflowSimulation> WorkflowSimulations => Set<WorkflowSimulation>();

    public DbSet<OperationRun> OperationRuns => Set<OperationRun>();

    public DbSet<DependentSystem> DependentSystems => Set<DependentSystem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
