using QRCoder;

namespace N4Sentinel.Web.Securite;

/// <summary>
/// SEC-001 — enrôlement d'une application d'authentification (TOTP, RFC 6238).
///
/// Le format <c>otpauth://</c> est un standard : Microsoft Authenticator, Google
/// Authenticator, FreeOTP et les autres lisent le même. Rien n'est spécifique à un éditeur, et
/// c'est voulu — lier l'exploitation à une application propriétaire serait une dépendance de
/// plus, sur un outil dont la raison d'être est de fonctionner quand le reste ne fonctionne pas.
///
/// Le code QR est rendu en SVG, dans le document. Ni image externe, ni service tiers : la
/// politique de contenu de l'application ne l'autoriserait pas, et surtout ce code **porte le
/// secret partagé**. Le faire calculer ailleurs reviendrait à l'y envoyer.
/// </summary>
public static class CodeQrDuSecondFacteur
{
    private const string Emetteur = "N4 Sentinel";

    /// <summary>
    /// URI d'enrôlement, à encoder dans le code QR. Contient le secret : elle ne doit jamais
    /// être journalisée, ni écrite au journal d'audit, ni transmise à un tiers.
    /// </summary>
    public static string ConstruireLUri(string compte, string cle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(compte);
        ArgumentException.ThrowIfNullOrWhiteSpace(cle);

        var etiquette = Uri.EscapeDataString($"{Emetteur}:{compte}");
        var emetteur = Uri.EscapeDataString(Emetteur);

        // Six chiffres, trente secondes : les valeurs par défaut d'ASP.NET Core Identity, donc
        // celles que la vérification appliquera. Les écrire évite qu'une application configurée
        // autrement produise des codes systématiquement refusés.
        return $"otpauth://totp/{etiquette}?secret={cle}&issuer={emetteur}&algorithm=SHA1&digits=6&period=30";
    }

    /// <summary>Code QR en SVG, prêt à être inséré dans la page.</summary>
    public static string ConstruireLeSvg(string compte, string cle)
    {
        using var generateur = new QRCodeGenerator();

        // Correction de niveau Q : un code affiché à l'écran puis photographié tolère mal les
        // reflets et les angles. Le gain de robustesse vaut la densité supplémentaire.
        using var donnees = generateur.CreateQrCode(
            ConstruireLUri(compte, cle), QRCodeGenerator.ECCLevel.Q);

        // Surcharge en hexadécimal plutôt qu'en System.Drawing.Color : celle-ci n'entraîne
        // aucune dépendance à System.Drawing, dont le support hors Windows est limité.
        return new SvgQRCode(donnees).GetGraphic(
            pixelsPerModule: 5,
            darkColorHex: "#0f172a",
            lightColorHex: "#ffffff",
            drawQuietZones: true,
            sizingMode: SvgQRCode.SizingMode.ViewBoxAttribute);
    }
}
