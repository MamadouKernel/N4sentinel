using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Habilitations;
using N4Sentinel.Application.Supervision;
using Microsoft.Extensions.Hosting;
using N4Sentinel.Data.Audit;
using N4Sentinel.Data.Habilitations;
using N4Sentinel.Application.Connecteurs;
using N4Sentinel.Application.Orchestration;
using N4Sentinel.Data.Orchestration;
using N4Sentinel.Data.Supervision;
using N4Sentinel.Data.Temps;

namespace N4Sentinel.Data;

/// <summary>Enregistrement de la couche Données / Audit.</summary>
public static class ServicesDeDonnees
{
    public static IServiceCollection AjouterLaCoucheDonnees(
        this IServiceCollection services,
        string chaineDeConnexion,
        OptionsDeCollecte? optionsDeCollecte = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chaineDeConnexion);

        services.AddSingleton<JournalEnAjoutSeulInterceptor>();

        // Une fabrique plutôt qu'un contexte partagé : en rendu Blazor, le gabarit et la page
        // s'initialisent en parallèle, et un DbContext ne supporte pas deux opérations
        // simultanées. Chaque composant crée le sien et le libère.
        services.AddDbContextFactory<ApplicationDbContext>((fournisseur, options) =>
        {
            options.UseSqlServer(chaineDeConnexion, sql => sql.EnableRetryOnFailure());
            options.AddInterceptors(fournisseur.GetRequiredService<JournalEnAjoutSeulInterceptor>());
        });

        // Identity et les points d'entrée HTTP attendent un contexte à durée de vie de requête :
        // il est produit par la même fabrique, et libéré par le conteneur en fin de requête.
        services.AddScoped(fournisseur =>
            fournisseur.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());

        services.AddSingleton<IClock, HorlogeSysteme>();
        services.AddScoped<IAuditTrail, PisteDAudit>();
        services.AddScoped<IServiceDHabilitations, ServiceDHabilitations>();
        services.AddScoped<ITesteurDeConfiguration, TesteurDeConfiguration>();
        services.AddScoped<IServiceDeSupervision, ServiceDeSupervision>();
        services.AddScoped<IEtatDExecutionPersiste, EtatDExecutionPersiste>();

        services.AddSingleton(optionsDeCollecte ?? new OptionsDeCollecte());
        services.AddHostedService<CollecteurPeriodique>();

        return services;
    }
}
