using N4Sentinel.Web.Securite;

namespace N4Sentinel.Application.Tests;

/// <summary>
/// SEC-001 — enrôlement d'une application d'authentification.
///
/// Ce qui est vérifié ici est l'URI, pas le dessin : c'est elle que l'application lit, et une
/// URI mal formée produit un compte qui génère des codes systématiquement refusés — panne
/// silencieuse, découverte à la première connexion, sur un outil d'exploitation.
/// </summary>
public class CodeQrDuSecondFacteurTests
{
    private const string Compte = "m.konate@cotedivoireterminal.com";
    private const string Cle = "JBSWY3DPEHPK3PXP";

    [Fact]
    public void L_uri_respecte_le_format_otpauth_attendu_par_les_applications()
    {
        var uri = new Uri(CodeQrDuSecondFacteur.ConstruireLUri(Compte, Cle));

        Assert.Equal("otpauth", uri.Scheme);
        Assert.Equal("totp", uri.Host);
    }

    [Fact]
    public void Le_secret_et_l_emetteur_sont_transmis()
    {
        var uri = CodeQrDuSecondFacteur.ConstruireLUri(Compte, Cle);

        Assert.Contains($"secret={Cle}", uri, StringComparison.Ordinal);
        Assert.Contains("issuer=N4%20Sentinel", uri, StringComparison.Ordinal);
    }

    [Fact]
    public void Les_parametres_correspondent_a_ce_que_la_verification_appliquera()
    {
        // Six chiffres, trente secondes, SHA-1 : les valeurs par défaut d'ASP.NET Core Identity.
        // Une application configurée autrement produirait des codes toujours refusés.
        var uri = CodeQrDuSecondFacteur.ConstruireLUri(Compte, Cle);

        Assert.Contains("digits=6", uri, StringComparison.Ordinal);
        Assert.Contains("period=30", uri, StringComparison.Ordinal);
        Assert.Contains("algorithm=SHA1", uri, StringComparison.Ordinal);
    }

    [Fact]
    public void L_adresse_du_compte_est_echappee_et_non_tronquee()
    {
        // L'arobase et le point ne doivent pas casser l'étiquette : une étiquette tronquée
        // donne un compte anonyme dans l'application, impossible à distinguer d'un autre.
        var uri = CodeQrDuSecondFacteur.ConstruireLUri(Compte, Cle);

        Assert.Contains(Uri.EscapeDataString($"N4 Sentinel:{Compte}"), uri, StringComparison.Ordinal);
    }

    [Fact]
    public void Le_code_qr_est_un_svg_autonome()
    {
        var svg = CodeQrDuSecondFacteur.ConstruireLeSvg(Compte, Cle);

        Assert.StartsWith("<svg", svg, StringComparison.Ordinal);
        Assert.Contains("viewBox", svg, StringComparison.Ordinal);

        // Aucune ressource externe n'est chargée : la politique de contenu l'interdirait, et
        // surtout ce code porte le secret partagé. La déclaration d'espace de noms XML, elle,
        // est une URL qui ne désigne rien à télécharger — c'est un identifiant, pas un lien.
        Assert.DoesNotContain("<image", svg, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("href", svg, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("url(", svg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Le_secret_n_apparait_jamais_en_clair_dans_le_dessin()
    {
        // Le secret est encodé dans les modules du code, pas écrit en toutes lettres dans le
        // balisage : une capture d'écran du SVG ne doit pas le livrer par simple lecture.
        var svg = CodeQrDuSecondFacteur.ConstruireLeSvg(Compte, Cle);

        Assert.DoesNotContain(Cle, svg, StringComparison.Ordinal);
        Assert.DoesNotContain(Compte, svg, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("", Cle)]
    [InlineData(Compte, "")]
    public void Un_enrolement_incomplet_est_refuse_plutot_que_produire_un_code_inutilisable(
        string compte, string cle)
    {
        Assert.Throws<ArgumentException>(() => CodeQrDuSecondFacteur.ConstruireLUri(compte, cle));
    }
}
