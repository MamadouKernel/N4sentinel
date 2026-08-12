using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Connectors;
using N4Sentinel.Data;
using N4Sentinel.Data.Orchestration;
using N4Sentinel.Data.Supervision;
using N4Sentinel.Orchestration;
using N4Sentinel.Application.Orchestration;
using N4Sentinel.Data.Amorcage;
using N4Sentinel.Data.Identite;
using N4Sentinel.Web.Components;
using N4Sentinel.Web.Administration;
using N4Sentinel.Web.Comptes;
using N4Sentinel.Web.Operations;
using N4Sentinel.Web.Pilotage;
using N4Sentinel.Web.Referentiel;
using N4Sentinel.Web.Supervision;
using N4Sentinel.Web.Courriel;
using N4Sentinel.Web.Securite;

var builder = WebApplication.CreateBuilder(args);

// Hébergement en service Windows, sans IIS : le processus est piloté par le gestionnaire de
// services, ce qui donne le démarrage automatique au boot du serveur applicatif.
builder.Host.UseWindowsService(options => options.ServiceName = "N4 Sentinel");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();

// — Couche Données / Audit —
var chaineDeConnexion = builder.Configuration["ChainesDeConnexion:BaseApplicative"];
if (string.IsNullOrWhiteSpace(chaineDeConnexion))
{
    throw new InvalidOperationException(
        "ChainesDeConnexion:BaseApplicative n'est pas renseignée. "
        + "L'application ne démarre pas sans base : l'audit ne peut pas être différé.");
}

// FR-053 — cadence de la collecte de fond, réglable par environnement d'exécution.
var optionsDeCollecte = new OptionsDeCollecte();
builder.Configuration.GetSection(OptionsDeCollecte.Section).Bind(optionsDeCollecte);

// Sprint 7 — cadence de l'avancement automatique des exécutions engagées.
var optionsDExecution = new OptionsDExecution();
builder.Configuration.GetSection(OptionsDExecution.Section).Bind(optionsDExecution);

builder.Services.AjouterLaCoucheDonnees(chaineDeConnexion, optionsDeCollecte, optionsDExecution);
builder.Services.AjouterLaCoucheConnecteurs();
builder.Services.AjouterLaCoucheOrchestrateur();

// — SEC-001 : identité applicative avec second facteur par courriel —
builder.Services.AddIdentity<UtilisateurApplicatif, IdentityRole>(options =>
{
    options.Password.RequiredLength = 12;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;

    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Le code de second facteur est envoyé par courriel : il doit expirer vite, sans quoi
// le second facteur ne protège plus que d'une négligence.
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
    options.TokenLifespan = TimeSpan.FromMinutes(5));

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/compte/connexion";
    options.LogoutPath = "/compte/deconnexion";
    options.AccessDeniedPath = "/compte/acces-refuse";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// SEC-002 — une politique par droit élémentaire.
//
// Pas de règle de repli globale : elle s'appliquerait aussi aux requêtes sans point d'entrée,
// et renverrait vers la page de connexion le script du cadriciel Blazor lui-même, cassant
// l'interactivité. L'exigence d'authentification est posée explicitement sur les pages et sur
// les groupes de points d'entrée, juste en dessous.
builder.Services.AddAuthorizationBuilder()
    .AjouterLesPolitiquesDeDroits();

// SEC-001 — assouplissements réservés au développement, refusés ailleurs.
var optionsDAuthentification = new OptionsDAuthentification();
builder.Configuration.GetSection(OptionsDAuthentification.Section).Bind(optionsDAuthentification);
optionsDAuthentification.Verifier(builder.Environment);
builder.Services.AddSingleton(optionsDAuthentification);

builder.Services.AddScoped<IUtilisateurCourant, UtilisateurCourant>();
builder.Services.AddSingleton<ISecretResolver, CoffreDeConfiguration>();

// — Messagerie —
var optionsDeCourriel = new OptionsDeCourriel();
builder.Configuration.GetSection("Courriel").Bind(optionsDeCourriel);
builder.Services.AddSingleton(optionsDeCourriel);

if (optionsDeCourriel.EstConfigure)
{
    builder.Services.AddScoped<IEnvoiDeCourriel, EnvoiDeCourrielSmtp>();
}
else
{
    var dossier = Path.Combine(builder.Environment.ContentRootPath, "courriels-sortants");
    builder.Services.AddScoped<IEnvoiDeCourriel>(fournisseur =>
        new EnvoiDeCourrielVersFichier(
            dossier,
            fournisseur.GetRequiredService<ILogger<EnvoiDeCourrielVersFichier>>()));
}

// — SEC-005 : chiffrement des données sensibles au repos —
var cheminDesCles = builder.Configuration["Securite:CheminDesClesDeProtection"];
if (!string.IsNullOrWhiteSpace(cheminDesCles))
{
    var protection = builder.Services.AddDataProtection()
        .SetApplicationName("N4Sentinel")
        .PersistKeysToFileSystem(new DirectoryInfo(cheminDesCles));

    if (OperatingSystem.IsWindows())
    {
        protection.ProtectKeysWithDpapi(protectToLocalMachine: true);
    }
}

// — SEC-005 : chiffrement des communications —
builder.Services.AddHttpsRedirection(options =>
{
    options.HttpsPort = builder.Configuration.GetValue<int?>("Securite:PortHttps") ?? 443;
});

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});

var app = builder.Build();

if (!optionsDeCourriel.EstConfigure)
{
    JournalDeCourriel.RelaisNonConfigure(
        app.Services.GetRequiredService<ILogger<EnvoiDeCourrielVersFichier>>());
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();
app.UseSecurityHeaders();

// Les ressources statiques sont servies avant l'autorisation. L'ordre n'est pas cosmétique :
// la règle de repli s'applique aussi aux requêtes qui ne correspondent à aucun point d'entrée,
// et renverrait donc la feuille de style de la page de connexion vers… la page de connexion.
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Toute page exige une authentification, sauf celles portant [AllowAnonymous] — connexion,
// second facteur, accès refusé, page introuvable. L'oubli d'un attribut ne peut donc pas
// ouvrir un écran.
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .RequireAuthorization();

app.MapperLesPointsDEntreeDeCompte();
app.MapperLesPointsDEntreeDeProfil();
app.MapperLePointDEntreeDeTheme();
app.MapperLesPointsDEntreeDuReferentiel();
app.MapperLesPointsDEntreeDeSupervision();
app.MapperLesPointsDEntreeDAdministration();
app.MapperLesPointsDEntreeDuPilotage();
app.MapperLesPointsDEntreeDesOperations();

// Sonde de disponibilité pour la supervision et le déploiement automatisé. Volontairement
// muette sur l'état interne : elle ne renseigne pas un appelant non authentifié.
app.MapGet("/sante", () => Results.Ok(new { statut = "ok" })).AllowAnonymous();

await AmorcageDeLIdentite.ExecuterAsync(
    app.Services,
    new ParametresDAmorcage(
        builder.Configuration["Amorcage:EmailAdministrateur"],
        builder.Configuration["Amorcage:MotDePasseAdministrateur"]));

// Après l'amorçage seulement — il applique les migrations, et la reprise lit des tables.
// Le moteur est persistant : il reprend en main les exécutions laissées « en cours » par un
// arrêt brutal du serveur applicatif, en recollectant l'état réel avant toute décision.
using (var portee = app.Services.CreateScope())
{
    var moteur = portee.ServiceProvider.GetRequiredService<IMoteurDOrchestration>();
    await moteur.RecupererLesExecutionsInterrompuesAsync();
}

await app.RunAsync();

/// <summary>Point d'entrée exposé aux tests d'intégration.</summary>
public partial class Program;
