using System.Security.Cryptography;

namespace N4Sentinel.Web.Securite;

/// <summary>
/// En-têtes de sécurité appliqués à toute réponse.
///
/// La politique de contenu n'autorise aucune origine externe : l'application est destinée à un
/// réseau CIT isolé, où un appel à un CDN ne se contenterait pas de fuiter une information —
/// il ne répondrait pas, et la page se dégraderait silencieusement. Polices, feuilles de style
/// et scripts sont donc servis par l'application elle-même.
///
/// Blazor émet une carte d'importation en ligne (« script type=importmap »), que « script-src
/// 'self' » bloque. Plutôt que d'ouvrir la politique à 'unsafe-inline' — ce qui autoriserait
/// alors tout script injecté —, un jeton aléatoire est tiré à chaque réponse et n'autorise que
/// les scripts en ligne qui le portent.
/// </summary>
public static class EntetesDeSecurite
{
    /// <summary>Clé sous laquelle le jeton est déposé pour la durée de la requête.</summary>
    public const string CleDuJeton = "csp-nonce";

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(async (context, next) =>
        {
            var jeton = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
            context.Items[CleDuJeton] = jeton;

            var entetes = context.Response.Headers;
            entetes["Content-Security-Policy"] = PolitiqueDeContenu(jeton);
            entetes["X-Content-Type-Options"] = "nosniff";
            entetes["X-Frame-Options"] = "DENY";
            entetes["Referrer-Policy"] = "no-referrer";
            entetes["Cross-Origin-Opener-Policy"] = "same-origin";
            entetes["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

            await next();
        });
    }

    /// <summary>Jeton de la requête en cours, à porter par les scripts en ligne légitimes.</summary>
    public static string? JetonDeScript(this HttpContext? context) =>
        context?.Items.TryGetValue(CleDuJeton, out var jeton) == true ? jeton as string : null;

    private static string PolitiqueDeContenu(string jeton) =>
        "default-src 'self'; "
        + $"script-src 'self' 'nonce-{jeton}'; "
        + "style-src 'self'; "
        + "img-src 'self' data:; "
        + "font-src 'self'; "
        + "connect-src 'self'; "
        + "frame-ancestors 'none'; "
        + "base-uri 'self'; "
        + "form-action 'self'";
}
