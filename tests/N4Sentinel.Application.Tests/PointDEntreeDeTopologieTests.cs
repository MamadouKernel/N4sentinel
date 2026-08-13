using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.EntityFrameworkCore;
using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Habilitations;

namespace N4Sentinel.Application.Tests;

/// <summary>
/// L'écran d'import, traversé par de vraies requêtes multipart.
///
/// Le point d'entrée porte un contrôle que ni le domaine ni l'import ne peuvent porter : le
/// droit vérifié <b>sur l'environnement visé</b>. Importer une topologie réécrit la
/// cartographie, donc l'ordre des séquences d'arrêt — un profil global n'y suffit pas.
/// </summary>
[Collection(CollectionDHoteHttp.Nom)]
public sealed class PointDEntreeDeTopologieTests(HoteDeTestHttp hote) : IClassFixture<HoteDeTestHttp>
{
    private static CancellationToken Jeton => TestContext.Current.CancellationToken;

    private const string FichierDeReference = """
        {
          "_comment": "Configuration Navis N4",
          "CenterNode": "N4CENTER01",
          "StandbyNode": "N4STANDBY01",
          "ClusterNodes": [ "N4CLUSTER01", "N4CLUSTER02", "N4CLUSTER03" ],
          "BridgeHost": "N4XPSBRIDGE01",
          "XPSHost": "N4XPSBRIDGE01",
          "ECN4Host": "N4ECN401",
          "ServiceNames": {
            "Center": "Navis N4 Center Node",
            "Cluster": "Navis N4 Cluster Node",
            "Standby": "Navis N4 Center Node",
            "Bridge": "Navis XPS Bridge Daemon",
            "XPS": "Navis XPS Service",
            "ECN4": "Navis ECN4 Daemon",
            "ECN4Web": "Navis ECN4web"
          },
          "SharedFolder": "\\\\N4CLUSTER01\\NavisShared",
          "DatabaseHost": "N4DB01",
          "DatabasePort": 1433,
          "LocalLogFolder": "C:\\NavisScripts\\Logs"
        }
        """;

    [Fact]
    public async Task Un_import_habilite_reprend_la_topologie_et_redirige_avec_son_bilan()
    {
        var uat = await hote.LireLEnvironnementAsync(EnvironmentType.Uat);
        var acteur = await hote.CreerUnUtilisateurAsync(
            "topologie.ok@test", "Administrateur référentiel",
            ProfilUtilisateur.AdministrateurDeLaSolution);
        await hote.HabiliterAsync(acteur, uat, ProfilUtilisateur.AdministrateurDeLaSolution);

        using var client = hote.CreerClient(acteur, "Administrateur référentiel");
        var reponse = await ImporterAsync(client, uat, FichierDeReference);

        Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
        var destination = reponse.Headers.Location!.ToString();
        Assert.Contains("importe=1", destination, StringComparison.Ordinal);
        Assert.Contains("crees=10", destination, StringComparison.Ordinal);
        Assert.Contains("sequences=2", destination, StringComparison.Ordinal);

        await using var contexte = hote.CreerLeContexte();
        var composants = await contexte.Composants.AsNoTracking()
            .Where(c => c.EnvironmentId == uat).ToListAsync(Jeton);

        Assert.Equal(10, composants.Count);
        // Tout en brouillon : l'écran n'active pas plus que le service.
        Assert.All(composants, c => Assert.Equal(ValidationStatus.Brouillon, c.Statut));
    }

    [Fact]
    public async Task Un_import_sans_habilitation_sur_l_environnement_est_refuse_et_trace()
    {
        var production = await hote.LireLEnvironnementAsync(EnvironmentType.Production);

        // Profil global d'administrateur de la solution, mais aucune habilitation sur
        // l'environnement visé : c'est exactement le cas que SEC-004 doit refuser.
        var acteur = await hote.CreerUnUtilisateurAsync(
            "topologie.sansdroit@test", "Administrateur global",
            ProfilUtilisateur.AdministrateurDeLaSolution);

        using var client = hote.CreerClient(acteur, "Administrateur global");
        var reponse = await ImporterAsync(client, production, FichierDeReference);

        Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
        Assert.Contains("erreur=droits", reponse.Headers.Location!.ToString(), StringComparison.Ordinal);

        await using var contexte = hote.CreerLeContexte();
        Assert.Equal(0, await contexte.Composants.CountAsync(c => c.EnvironmentId == production, Jeton));

        Assert.True(await contexte.EntreesDAudit.AnyAsync(
            e => !e.Autorisee && e.MotifDeRefus != null
                 && e.MotifDeRefus.Contains("Import de topologie refusé"), Jeton));
    }

    [Fact]
    public async Task Un_fichier_illisible_est_refuse_sans_rien_ecrire_et_le_motif_reste_au_journal()
    {
        var uat = await hote.LireLEnvironnementAsync(EnvironmentType.Uat);
        var acteur = await hote.CreerUnUtilisateurAsync(
            "topologie.json@test", "Opérateur", ProfilUtilisateur.AdministrateurDeLaSolution);
        await hote.HabiliterAsync(acteur, uat, ProfilUtilisateur.AdministrateurDeLaSolution);

        using var client = hote.CreerClient(acteur, "Opérateur");
        var reponse = await ImporterAsync(client, uat, "{ ceci n'est pas du JSON");

        Assert.Contains("erreur=json", reponse.Headers.Location!.ToString(), StringComparison.Ordinal);

        await using var contexte = hote.CreerLeContexte();
        Assert.True(await contexte.EntreesDAudit.AnyAsync(
            e => !e.Autorisee && e.MotifDeRefus != null
                 && e.MotifDeRefus.Contains("fichier illisible"), Jeton));
    }

    private static async Task<HttpResponseMessage> ImporterAsync(
        HttpClient client,
        Guid environnementId,
        string contenu)
    {
        var jeton = await HoteDeTestHttp.LireLeJetonAsync(client);

        using var corps = new MultipartFormDataContent
        {
            { new StringContent(jeton), "__RequestVerificationToken" },
            { new StringContent(environnementId.ToString()), "environnementId" },
            { new StringContent("true"), "genererLesSequences" }
        };

        var fichier = new ByteArrayContent(Encoding.UTF8.GetBytes(contenu));
        fichier.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        corps.Add(fichier, "fichier", "Navis-Config.json");

        return await client.PostAsync("/referentiel/topologie/importer", corps, Jeton);
    }
}
