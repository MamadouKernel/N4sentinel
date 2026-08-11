namespace N4Sentinel.Web.Security;

/// <summary>
/// En-têtes de sécurité appliqués à toute réponse.
///
/// La politique de contenu n'autorise aucune origine externe : l'application est destinée à un
/// réseau CIT isolé, où un appel à un CDN ne se contenterait pas de fuiter une information —
/// il ne répondrait pas, et la page se dégraderait silencieusement. Polices, feuilles de style
/// et scripts sont donc servis par l'application elle-même.
/// </summary>
public static class SecurityHeadersMiddleware
{
    private const string PolitiqueDeContenu =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self'; " +
        "img-src 'self' data:; " +
        "font-src 'self'; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'";

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var entetes = context.Response.Headers;
            entetes["Content-Security-Policy"] = PolitiqueDeContenu;
            entetes["X-Content-Type-Options"] = "nosniff";
            entetes["X-Frame-Options"] = "DENY";
            entetes["Referrer-Policy"] = "no-referrer";
            entetes["Cross-Origin-Opener-Policy"] = "same-origin";
            entetes["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

            await next();
        });
}
