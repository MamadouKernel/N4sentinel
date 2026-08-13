using Microsoft.EntityFrameworkCore;

namespace N4Sentinel.Application.Tests;

/// <summary>
/// SEC-001 — écart validé par la DSI : l'utilisateur peut renoncer au second facteur.
///
/// C'est le geste le plus sensible que l'application expose, puisqu'il abaisse le niveau
/// d'authentification d'un compte capable d'arrêter des systèmes de production. Faute de
/// pouvoir l'empêcher, deux propriétés doivent tenir : il ne se produit jamais par
/// inadvertance, et il ne se produit jamais sans trace.
/// </summary>
[Collection(CollectionDHoteHttp.Nom)]
public sealed class ActivationDuSecondFacteurTests(HoteDeTestHttp hote) : IClassFixture<HoteDeTestHttp>
{
    private static CancellationToken Jeton => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Desactiver_sans_cocher_la_confirmation_est_refuse_et_trace()
    {
        var acteur = await hote.CreerUnUtilisateurAsync("2fa.sansconfirmation@test", "Opérateur");
        await hote.PoserLeSecondFacteurAsync(acteur, actif: true);

        using var client = hote.CreerClient(acteur, "Opérateur");
        var reponse = await HoteDeTestHttp.PosterAsync(
            client, "/compte/profil/second-facteur/activation",
            new Dictionary<string, string> { ["activer"] = "false" });

        Assert.Equal("/compte/profil?erreur=confirmation", reponse.Headers.Location!.ToString());

        // La propriété qui compte : le compte est toujours protégé.
        Assert.True(await hote.LeSecondFacteurEstActifAsync(acteur));

        await using var contexte = hote.CreerLeContexte();
        Assert.True(await contexte.EntreesDAudit.AnyAsync(
            e => !e.Autorisee && e.MotifDeRefus != null
                 && e.MotifDeRefus.Contains("confirmation explicite non cochée"), Jeton));
    }

    [Fact]
    public async Task Desactiver_avec_confirmation_est_applique_et_trace()
    {
        var acteur = await hote.CreerUnUtilisateurAsync("2fa.desactivation@test", "Opérateur consentant");
        await hote.PoserLeSecondFacteurAsync(acteur, actif: true);

        using var client = hote.CreerClient(acteur, "Opérateur consentant");
        var reponse = await HoteDeTestHttp.PosterAsync(
            client, "/compte/profil/second-facteur/activation",
            new Dictionary<string, string> { ["activer"] = "false", ["confirmation"] = "true" });

        Assert.Equal("/compte/profil?message=second-facteur-desactive", reponse.Headers.Location!.ToString());
        Assert.False(await hote.LeSecondFacteurEstActifAsync(acteur));

        // Un renoncement sans trace serait indéfendable en recette : c'est ce qui distingue
        // un écart assumé d'une régression silencieuse.
        await using var contexte = hote.CreerLeContexte();
        Assert.True(await contexte.EntreesDAudit.AnyAsync(
            e => e.IdentifiantDObjet == acteur
                 && e.ValeurApres != null && e.ValeurApres.Contains("écart à SEC-001"), Jeton));
    }

    [Fact]
    public async Task Reactiver_ne_demande_aucune_confirmation()
    {
        // Remonter le niveau de sécurité n'a pas à être découragé par une friction.
        var acteur = await hote.CreerUnUtilisateurAsync("2fa.reactivation@test", "Opérateur prudent");
        await hote.PoserLeSecondFacteurAsync(acteur, actif: false);

        using var client = hote.CreerClient(acteur, "Opérateur prudent");
        var reponse = await HoteDeTestHttp.PosterAsync(
            client, "/compte/profil/second-facteur/activation",
            new Dictionary<string, string> { ["activer"] = "true" });

        Assert.Equal("/compte/profil?message=second-facteur-active", reponse.Headers.Location!.ToString());
        Assert.True(await hote.LeSecondFacteurEstActifAsync(acteur));
    }

    [Fact]
    public async Task Demander_l_etat_deja_en_place_ne_change_rien_et_ne_trace_rien()
    {
        var acteur = await hote.CreerUnUtilisateurAsync("2fa.idempotent@test", "Opérateur");
        await hote.PoserLeSecondFacteurAsync(acteur, actif: true);

        using var client = hote.CreerClient(acteur, "Opérateur");
        var reponse = await HoteDeTestHttp.PosterAsync(
            client, "/compte/profil/second-facteur/activation",
            new Dictionary<string, string> { ["activer"] = "true" });

        Assert.Equal("/compte/profil", reponse.Headers.Location!.ToString());
        Assert.True(await hote.LeSecondFacteurEstActifAsync(acteur));

        await using var contexte = hote.CreerLeContexte();
        Assert.False(await contexte.EntreesDAudit.AnyAsync(
            e => e.IdentifiantDObjet == acteur, Jeton));
    }
}
