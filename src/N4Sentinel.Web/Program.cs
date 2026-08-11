using Microsoft.AspNetCore.DataProtection;
using N4Sentinel.Web.Components;
using N4Sentinel.Web.Security;

var builder = WebApplication.CreateBuilder(args);

// Hébergement en service Windows, sans IIS : le processus est piloté par le gestionnaire de
// services, ce qui donne le démarrage automatique au boot du serveur applicatif.
builder.Host.UseWindowsService(options => options.ServiceName = "N4 Sentinel");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// SEC-005 — chiffrement des données sensibles au repos.
// Les clés de protection (cookies d'authentification, jetons antiforgery, valeurs protégées)
// sont persistées hors du répertoire applicatif, puis chiffrées par DPAPI au niveau machine :
// une copie du dossier de clés sur un autre serveur est inexploitable.
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

// SEC-005 — chiffrement des communications. Le port HTTPS est déclaré explicitement pour que
// la redirection connaisse sa cible en service Windows, c'est-à-dire sans les variables
// d'environnement que fournirait un hébergement IIS.
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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseSecurityHeaders();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Sonde de disponibilité pour la supervision et le déploiement automatisé. Volontairement
// muette sur l'état interne : elle ne renseigne pas un appelant non authentifié.
app.MapGet("/sante", () => Results.Ok(new { statut = "ok" }));

app.Run();

/// <summary>Point d'entrée exposé aux tests d'intégration.</summary>
public partial class Program;
