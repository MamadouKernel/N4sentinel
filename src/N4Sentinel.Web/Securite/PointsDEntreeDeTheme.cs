using Microsoft.AspNetCore.Mvc;

namespace N4Sentinel.Web.Securite;

/// <summary>
/// Choix du thème d'affichage. Accessible sans authentification : la page de connexion doit
/// pouvoir basculer, sans quoi le réglage ne s'appliquerait qu'une fois connecté.
/// </summary>
public static class PointsDEntreeDeTheme
{
    public static IEndpointRouteBuilder MapperLePointDEntreeDeTheme(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        routes.MapPost("/theme", (
            [FromForm] string theme,
            [FromForm] string? retour,
            HttpContext contexte) =>
        {
            contexte.Response.Cookies.Append(
                "theme",
                theme == "clair" ? "clair" : "sombre",
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    MaxAge = TimeSpan.FromDays(365)
                });

            // Retour local seulement. Une URL absolue postée depuis l'extérieur ferait de ce
            // point d'entrée une redirection ouverte, exploitable en hameçonnage.
            return Results.LocalRedirect(
                string.IsNullOrWhiteSpace(retour) || !retour.StartsWith('/') ? "/" : retour);
        }).AllowAnonymous();

        return routes;
    }
}
