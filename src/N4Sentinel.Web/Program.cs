using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Diagnostics.Queries;
using N4Sentinel.Application.Sops.Queries;
using N4Sentinel.Infrastructure;
using N4Sentinel.Web.Components;
using N4Sentinel.Web.Components.Account;
using N4Sentinel.Web.Configuration;
using N4Sentinel.Web.Data;
using N4Sentinel.Web.Reports;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Serilog;

// FR-090 : export PDF réel. Licence Community QuestPDF — gratuite pour une entité dont le chiffre d'affaires
// annuel brut est inférieur à 1M$ US (cas d'un outil interne CIT non commercialisé).
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<BrandingOptions>(builder.Configuration.GetSection(BrandingOptions.SectionName));
builder.Services.Configure<FeatureOptions>(builder.Configuration.GetSection(FeatureOptions.SectionName));

// Hébergement en Service Windows plutôt qu'IIS : no-op automatique hors installation en tant que service
// (dotnet run en développement n'est pas affecté). Nom de service utilisé par les scripts install-service.ps1.
builder.Host.UseWindowsService(options => options.ServiceName = "N4Sentinel");

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();
builder.Services.AddAuthorization();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory_Identity")));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
builder.Services.AddScoped<IUserRoleService, UserRoleService>();
builder.Services.AddScoped<IEnvironmentAccessChecker, EnvironmentAccessChecker>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();

    using var scope = app.Services.CreateScope();
    await IdentitySeeder.SeedAsync(scope.ServiceProvider, app.Configuration);
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// Pas de redirection HTTPS/HSTS : la solution est hébergée en HTTP sur le réseau interne CIT, sans IIS ni
// terminaison TLS pour l'instant (décision DSI). À revoir si l'accès s'étend hors du réseau de confiance.

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

// FR-093/E10.2 : export structuré des rapports d'opération/incident (vue rendue, aucune entité "Rapport" persistée).
var reportJsonOptions = new JsonSerializerOptions { WriteIndented = true };

app.MapGet("/reports/operations/{operationRunId:guid}/export", async (Guid operationRunId, ISender mediator) =>
{
    var report = await mediator.Send(new GetOperationReportQuery(operationRunId));
    return report is null
        ? Results.NotFound()
        : Results.File(
            JsonSerializer.SerializeToUtf8Bytes(report, reportJsonOptions), "application/json",
            $"rapport-operation-{operationRunId}.json");
}).RequireAuthorization();

app.MapGet("/reports/operations/{operationRunId:guid}/export.pdf", async (Guid operationRunId, ISender mediator) =>
{
    var report = await mediator.Send(new GetOperationReportQuery(operationRunId));
    return report is null
        ? Results.NotFound()
        : Results.File(
            new OperationReportPdfDocument(report).GeneratePdf(), "application/pdf",
            $"rapport-operation-{operationRunId}.pdf");
}).RequireAuthorization();

app.MapGet("/reports/incidents/{diagnosticCaseId:guid}/export", async (Guid diagnosticCaseId, ISender mediator) =>
{
    var report = await mediator.Send(new GetIncidentReportQuery(diagnosticCaseId));
    return report is null
        ? Results.NotFound()
        : Results.File(
            JsonSerializer.SerializeToUtf8Bytes(report, reportJsonOptions), "application/json",
            $"rapport-incident-{diagnosticCaseId}.json");
}).RequireAuthorization();

app.MapGet("/reports/incidents/{diagnosticCaseId:guid}/export.pdf", async (Guid diagnosticCaseId, ISender mediator) =>
{
    var report = await mediator.Send(new GetIncidentReportQuery(diagnosticCaseId));
    return report is null
        ? Results.NotFound()
        : Results.File(
            new IncidentReportPdfDocument(report).GeneratePdf(), "application/pdf",
            $"rapport-incident-{diagnosticCaseId}.pdf");
}).RequireAuthorization();

// FR-067 : export structuré du paquet d'escalade (journaux déjà expurgés à l'import, empreintes SHA-256 déjà calculées).
app.MapGet("/reports/incidents/{diagnosticCaseId:guid}/escalation-package/export", async (Guid diagnosticCaseId, ISender mediator, HttpContext context) =>
{
    var userName = context.User.Identity?.Name ?? "inconnu";
    var package = await mediator.Send(new GetEscalationPackageQuery(diagnosticCaseId, userName));
    return package is null
        ? Results.NotFound()
        : Results.File(
            JsonSerializer.SerializeToUtf8Bytes(package, reportJsonOptions), "application/json",
            $"paquet-escalade-{diagnosticCaseId}.json");
}).RequireAuthorization();

app.Run();
