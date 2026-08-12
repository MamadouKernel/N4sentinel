using Microsoft.Extensions.DependencyInjection;
using N4Sentinel.Application.Connecteurs;

namespace N4Sentinel.Connectors;

/// <summary>Enregistrement de la couche Connecteurs.</summary>
public static class ServicesDeConnecteurs
{
    public static IServiceCollection AjouterLaCoucheConnecteurs(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpClient<ConnecteurHttp>();

        services.AddScoped<IConnecteurDeSignaux, ConnecteurTcp>();
        services.AddScoped<IConnecteurDeSignaux, ConnecteurDeDossier>();
        services.AddScoped<IConnecteurDeSignaux, ConnecteurSqlLectureSeule>();
        services.AddScoped<IConnecteurDeSignaux>(f => f.GetRequiredService<ConnecteurHttp>());

        // Le connecteur de service Windows n'existe que là où le concept existe.
        if (OperatingSystem.IsWindows())
        {
            services.AddScoped<IConnecteurDeSignaux, ConnecteurDeServiceWindows>();
        }

        // Les Cluster Services N4 demandent un accès au cluster dont le projet ne dispose pas.
        // Plutôt que d'omettre le type — ce qui donnerait « aucun connecteur » sans expliquer
        // pourquoi —, un connecteur qui dit explicitement ce qui manque est enregistré.
        services.AddScoped<IConnecteurDeSignaux>(_ => new ConnecteurDeSimulation(
            TypesDeControle.ClusterServices,
            "lecture des Cluster Services non disponible : accès au cluster N4 non ouvert. "
            + "Aucun état n'est supposé."));

        services.AddScoped<IRepartiteurDeConnecteurs>(f =>
            new RepartiteurDeConnecteurs(f.GetServices<IConnecteurDeSignaux>()));

        return services;
    }
}
