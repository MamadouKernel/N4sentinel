using Microsoft.Extensions.DependencyInjection;
using N4Sentinel.Application.Orchestration;

namespace N4Sentinel.Orchestration;

/// <summary>Enregistrement de la couche Orchestrateur.</summary>
public static class ServicesDOrchestration
{
    public static IServiceCollection AjouterLaCoucheOrchestrateur(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IMoteurDOrchestration, MoteurDOrchestration>();

        return services;
    }
}
