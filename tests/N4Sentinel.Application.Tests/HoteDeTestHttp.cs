using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using N4Sentinel.Data;
using N4Sentinel.Data.Identite;
using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Habilitations;

namespace N4Sentinel.Application.Tests;

/// <summary>
/// Regroupe les classes qui montent un hôte ASP.NET, pour qu'elles s'exécutent l'une après
/// l'autre. Deux <c>WebApplicationFactory</c> démarrées en parallèle invoquent par réflexion le
/// même point d'entrée applicatif, et cette initialisation concurrente échoue de façon
/// intermittente — chaque classe passe seule, plusieurs ensemble non.
///
/// Le regroupement ne partage aucun état : chaque classe conserve sa propre base par
/// <c>IClassFixture</c>. Seule l'exécution est sérialisée.
/// </summary>
[CollectionDefinition(Nom)]
public sealed class CollectionDHoteHttp
{
    public const string Nom = "Hôte HTTP";
}

/// <summary>
/// Hôte HTTP réel de l'application, servi en mémoire, sur une base dédiée.
///
/// Ce que les tests de moteur ne peuvent pas atteindre vit ici : les points d'entrée portent les
/// contrôles d'habilitation par environnement (SEC-004), la séparation des responsabilités et
/// l'écriture au journal d'audit des refus. Vérifier ces règles en lisant le code n'est pas les
/// vérifier — c'est le pipeline complet, autorisation et antiforgery comprises, qui décide.
///
/// L'authentification est remplacée par un schéma de test : l'acteur est désigné par un en-tête,
/// et rien d'autre ne change. Un mot de passe n'a aucun rôle dans ce qui est vérifié ici, et le
/// mécanisme d'ouverture de session est déjà couvert par le parcours de connexion du Sprint 1.
/// </summary>
public sealed partial class HoteDeTestHttp : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string EnTeteActeur = "X-Test-Acteur";
    public const string EnTeteNom = "X-Test-Nom";

    private readonly string nomDeLaBase = "N4Sentinel_Http_" + Guid.NewGuid().ToString("N")[..12];

    private static string Serveur =>
        Environment.GetEnvironmentVariable("N4SENTINEL_TESTS_SQLSERVER") is { Length: > 0 } serveur
            ? serveur
            : ".";

    private string ChaineDeConnexion =>
        $"Server={Serveur};Database={nomDeLaBase};Trusted_Connection=True;"
        + "MultipleActiveResultSets=true;TrustServerCertificate=True";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Development");

        // Ajoutée en dernier, donc prioritaire sur appsettings.Development.json et sur les
        // secrets utilisateur : sans cela, les tests écriraient dans la base de développement.
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ChainesDeConnexion:BaseApplicative"] = ChaineDeConnexion,

                // Les deux boucles de fond rendraient les assertions non déterministes : une
                // étape pourrait avancer entre l'action et la vérification.
                ["Supervision:CollecteAutomatiqueActive"] = "false",
                ["Execution:ExecutionAutomatiqueActive"] = "false",

                // Aucun compte d'amorçage : les tests créent les leurs, avec les seuls droits
                // que chaque cas exige. Un administrateur passe-partout masquerait les refus.
                ["Amorcage:EmailAdministrateur"] = "",
                ["Amorcage:MotDePasseAdministrateur"] = ""
            }));

        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(SchemaDeTest.Nom)
                .AddScheme<AuthenticationSchemeOptions, SchemaDeTest>(SchemaDeTest.Nom, _ => { });

            // AddIdentity a déjà posé ses schémas par défaut ; la dernière configuration gagne.
            services.Configure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = SchemaDeTest.Nom;
                options.DefaultChallengeScheme = SchemaDeTest.Nom;
                options.DefaultScheme = SchemaDeTest.Nom;
            });

            services.AddLogging(journal => journal.SetMinimumLevel(LogLevel.Warning));
        });
    }

    /// <summary>Client parlant en HTTPS : la redirection et le cookie « Secure » l'exigent.</summary>
    public HttpClient CreerClient(string acteurId, string nomAffiche)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

        client.DefaultRequestHeaders.Add(EnTeteActeur, acteurId);
        client.DefaultRequestHeaders.Add(EnTeteNom, nomAffiche);

        return client;
    }

    public ApplicationDbContext CreerLeContexte()
    {
        var portee = Services.CreateScope();
        return portee.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    /// <summary>
    /// Crée un acteur et lui pose un profil global. Le profil par défaut n'accorde que la
    /// consultation : c'est ce que les groupes de points d'entrée exigent pour laisser passer
    /// la requête, tout geste mutatif restant conditionné à une habilitation d'environnement
    /// (SEC-004). Donner d'emblée un profil puissant masquerait précisément ce qu'on teste.
    /// </summary>
    public async Task<string> CreerUnUtilisateurAsync(
        string email,
        string nomComplet,
        ProfilUtilisateur profilGlobal = ProfilUtilisateur.LecteurSupportN1)
    {
        using var portee = Services.CreateScope();
        var gestionnaire = portee.ServiceProvider.GetRequiredService<UserManager<UtilisateurApplicatif>>();

        var utilisateur = new UtilisateurApplicatif
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            NomComplet = nomComplet,
            Fonction = "Test"
        };

        // Sans mot de passe : ces tests vérifient l'autorisation, jamais l'ouverture de session.
        var resultat = await gestionnaire.CreateAsync(utilisateur);
        Assert.True(resultat.Succeeded, string.Join(" ; ", resultat.Errors.Select(e => e.Description)));

        var ajout = await gestionnaire.AddToRoleAsync(utilisateur, profilGlobal.ToString());
        Assert.True(ajout.Succeeded, string.Join(" ; ", ajout.Errors.Select(e => e.Description)));

        return utilisateur.Id;
    }

    /// <summary>Pose l'état du second facteur, que la création de compte laisse à faux.</summary>
    public async Task PoserLeSecondFacteurAsync(string utilisateurId, bool actif)
    {
        using var portee = Services.CreateScope();
        var gestionnaire = portee.ServiceProvider.GetRequiredService<UserManager<UtilisateurApplicatif>>();

        var utilisateur = await gestionnaire.FindByIdAsync(utilisateurId);
        Assert.NotNull(utilisateur);

        await gestionnaire.SetTwoFactorEnabledAsync(utilisateur, actif);
    }

    public async Task<bool> LeSecondFacteurEstActifAsync(string utilisateurId)
    {
        using var portee = Services.CreateScope();
        var gestionnaire = portee.ServiceProvider.GetRequiredService<UserManager<UtilisateurApplicatif>>();

        var utilisateur = await gestionnaire.FindByIdAsync(utilisateurId);
        Assert.NotNull(utilisateur);

        return await gestionnaire.GetTwoFactorEnabledAsync(utilisateur);
    }

    public async Task HabiliterAsync(string utilisateurId, Guid environnementId, ProfilUtilisateur profil)
    {
        using var portee = Services.CreateScope();
        var contexte = portee.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        contexte.Habilitations.Add(new HabilitationEnvironnement
        {
            UtilisateurId = utilisateurId,
            EnvironmentId = environnementId,
            Profil = profil,
            AccordeePar = "Test"
        });

        await contexte.SaveChangesAsync();
    }

    public async Task<Guid> LireLEnvironnementAsync(EnvironmentType type)
    {
        using var portee = Services.CreateScope();
        var contexte = portee.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await contexte.Environnements.Where(e => e.Type == type).Select(e => e.Id).FirstAsync();
    }

    /// <summary>
    /// Jeton antiforgery lié à l'acteur du client. Il est lu sur une page rendue pour ce même
    /// acteur : un jeton obtenu anonymement serait rejeté, l'identité étant scellée dedans.
    /// </summary>
    public static async Task<string> LireLeJetonAsync(HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        var page = await client.GetStringAsync("/");
        var trouve = JetonAntiforgery().Match(page);

        Assert.True(trouve.Success, "Aucun jeton antiforgery trouvé sur la page d'accueil.");
        return trouve.Groups["jeton"].Value;
    }

    public static async Task<HttpResponseMessage> PosterAsync(
        HttpClient client,
        string chemin,
        Dictionary<string, string> champs)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(champs);

        var corps = new Dictionary<string, string>(champs)
        {
            ["__RequestVerificationToken"] = await LireLeJetonAsync(client)
        };

        var requete = new HttpRequestMessage(HttpMethod.Post, chemin)
        {
            Content = new FormUrlEncodedContent(corps)
        };
        requete.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        return await client.SendAsync(requete);
    }

    [GeneratedRegex(
        """name="__RequestVerificationToken"[^>]*value="(?<jeton>[^"]+)""",
        RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 500)]
    private static partial Regex JetonAntiforgery();

    public ValueTask InitializeAsync()
    {
        // Le démarrage de l'application applique les migrations, crée les huit profils et les
        // environnements Production et UAT : les tests s'appuient dessus plutôt que de les
        // recréer, ce qui exercerait autre chose que ce que l'application fait vraiment.
        _ = Services;
        return ValueTask.CompletedTask;
    }

    public override async ValueTask DisposeAsync()
    {
        await using (var contexte = CreerLeContexte())
        {
            await contexte.Database.EnsureDeletedAsync();
        }

        await base.DisposeAsync();
    }
}

/// <summary>
/// Schéma d'authentification de test : l'acteur est celui que l'en-tête désigne. Les claims
/// posées sont exactement celles que lit <c>UtilisateurCourant</c> — identifiant et nom.
/// </summary>
internal sealed class SchemaDeTest(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory journal,
    UrlEncoder encodeur) : AuthenticationHandler<AuthenticationSchemeOptions>(options, journal, encodeur)
{
    public const string Nom = "TestDAutorisation";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HoteDeTestHttp.EnTeteActeur, out var acteur)
            || string.IsNullOrEmpty(acteur))
        {
            return AuthenticateResult.NoResult();
        }

        var nom = Request.Headers.TryGetValue(HoteDeTestHttp.EnTeteNom, out var valeur)
            ? valeur.ToString()
            : acteur.ToString();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, acteur!),
            new(ClaimTypes.Name, nom)
        };

        // Les politiques de droits reposent sur les rôles globaux (RequireRole) : sans ces
        // claims, tout point d'entrée répondrait 403 avant même d'atteindre son handler, et le
        // test ne vérifierait plus la règle métier mais l'absence de rôle.
        var gestionnaire = Context.RequestServices.GetRequiredService<UserManager<UtilisateurApplicatif>>();
        if (await gestionnaire.FindByIdAsync(acteur!) is { } utilisateur)
        {
            foreach (var role in await gestionnaire.GetRolesAsync(utilisateur))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        var identite = new ClaimsIdentity(claims, Nom);

        return AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identite), Nom));
    }
}
