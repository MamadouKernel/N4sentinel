using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Audit;
using N4Sentinel.Application.Habilitations;
using N4Sentinel.Application.Referentiel;
using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Habilitations;
using N4Sentinel.Domain.Referentiel;
using N4Sentinel.Web.Securite;

namespace N4Sentinel.Web.Referentiel;

/// <summary>
/// Import d'une topologie décrite par la configuration des scripts d'exploitation N4 (SOP-2).
///
/// Le droit est revérifié <b>sur l'environnement visé</b> et non seulement sur le groupe :
/// importer une topologie réécrit le référentiel, donc l'ordre des séquences d'arrêt, donc ce
/// que l'application s'autorise à faire en Production. Un profil global ne suffit pas (SEC-004).
/// </summary>
public static class PointsDEntreeDeTopologie
{
    /// <summary>
    /// Le fichier de référence pèse un peu plus d'un kilo-octet. Cent kilo-octets laissent la
    /// place à une topologie très large tout en refusant qu'on téléverse autre chose.
    /// </summary>
    private const long TailleMaximale = 100 * 1024;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static IEndpointRouteBuilder MapperLesPointsDEntreeDeTopologie(
        this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        routes.MapGroup("/referentiel/topologie")
            .RequireAuthorization(PolitiquesDAutorisation.NomDe(Droit.GererLeReferentiel))
            .MapPost("/importer", ImporterAsync)
            // Le fichier est lu en mémoire : la borne est posée par le serveur, pas par la
            // bonne volonté de l'appelant.
            .WithMetadata(new RequestSizeLimitAttribute(TailleMaximale));

        return routes;
    }

    private static async Task<IResult> ImporterAsync(
        [FromForm] Guid environnementId,
        IFormFile? fichier,
        IImportDeTopologie import,
        IServiceDHabilitations habilitations,
        IUtilisateurCourant acteur,
        IAuditTrail piste,
        [FromForm] bool genererLesSequences = false)
    {
        var identifiant = acteur.Identifiant ?? string.Empty;

        if (!await habilitations.AutoriseAsync(identifiant, environnementId, Droit.GererLeReferentiel))
        {
            await TracerLeRefusAsync(piste, acteur, environnementId,
                "Import de topologie refusé : droit GererLeReferentiel manquant sur cet environnement.");
            return Redirection(environnementId, "erreur=droits");
        }

        if (fichier is null || fichier.Length == 0)
        {
            return Redirection(environnementId, "erreur=fichier");
        }

        if (fichier.Length > TailleMaximale)
        {
            await TracerLeRefusAsync(piste, acteur, environnementId,
                $"Import de topologie refusé : fichier de {fichier.Length} octets, au-delà de la limite.");
            return Redirection(environnementId, "erreur=taille");
        }

        ConfigurationN4? configuration;
        try
        {
            await using var flux = fichier.OpenReadStream();
            configuration = await JsonSerializer.DeserializeAsync<ConfigurationN4>(flux, Options);
        }
        catch (JsonException erreur)
        {
            // Le motif exact part au journal ; l'écran reste sobre. Un message d'analyse JSON
            // renvoyé tel quel à la page dirait à un appelant non authentifié ce qu'on a lu.
            await TracerLeRefusAsync(piste, acteur, environnementId,
                $"Import de topologie refusé : fichier illisible — {erreur.Message}");
            return Redirection(environnementId, "erreur=json");
        }

        var rapport = await import.ImporterAsync(environnementId, configuration!, genererLesSequences);

        if (!rapport.Applique)
        {
            return Redirection(environnementId, "erreur=vide");
        }

        return Redirection(environnementId,
            $"importe=1&crees={rapport.ComposantsCrees}&majs={rapport.ComposantsMisAJour}"
            + $"&inchanges={rapport.ComposantsInchanges}&sequences={rapport.WorkflowsGeneres.Count}");
    }

    private static IResult Redirection(Guid environnementId, string parametres) =>
        Results.Redirect($"/referentiel/topologie?environnementId={environnementId}&{parametres}");

    private static Task TracerLeRefusAsync(
        IAuditTrail piste,
        IUtilisateurCourant acteur,
        Guid environnementId,
        string motif) =>
        piste.EnregistrerAsync(new AuditEntry
        {
            Acteur = acteur.NomAffiche,
            Action = ActionsAuditees.ModificationRefusee,
            TypeDObjet = ObjetsAudites.Composant,
            IdentifiantDObjet = environnementId.ToString(),
            EnvironmentId = environnementId,
            AdresseIp = acteur.AdresseIp,
            Autorisee = false,
            MotifDeRefus = motif,
            Origine = AuditOrigin.InterfaceWeb
        });
}
