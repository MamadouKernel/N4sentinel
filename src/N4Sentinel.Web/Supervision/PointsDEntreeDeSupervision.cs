using Microsoft.AspNetCore.Mvc;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Audit;
using N4Sentinel.Application.Supervision;
using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Habilitations;
using N4Sentinel.Web.Securite;

namespace N4Sentinel.Web.Supervision;

/// <summary>
/// FR-053 — rafraîchissement à la demande, en complément de la collecte automatique.
///
/// La collecte interroge de vrais serveurs : elle est donc réservée au droit de gestion du
/// référentiel et tracée. Laisser n'importe quel lecteur la déclencher exposerait les
/// composants supervisés à autant d'appels que de clics.
/// </summary>
public static class PointsDEntreeDeSupervision
{
    public static IEndpointRouteBuilder MapperLesPointsDEntreeDeSupervision(
        this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        routes.MapPost("/supervision/collecte", async (
            [FromForm] Guid environnementId,
            IServiceDeSupervision supervision,
            IUtilisateurCourant acteur,
            IAuditTrail piste,
            CancellationToken cancellationToken) =>
        {
            var releves = await supervision.CollecterAsync(environnementId, cancellationToken);

            await piste.EnregistrerAsync(new AuditEntry
            {
                Acteur = acteur.NomAffiche,
                Action = ActionsAuditees.CollecteDemandee,
                TypeDObjet = ObjetsAudites.Environnement,
                IdentifiantDObjet = environnementId.ToString(),
                EnvironmentId = environnementId,
                ValeurApres = $"{releves} relevé(s)",
                AdresseIp = acteur.AdresseIp,
                Origine = AuditOrigin.InterfaceWeb
            }, cancellationToken);

            return Results.Redirect("/supervision?message=collecte");
        }).RequireAuthorization(PolitiquesDAutorisation.NomDe(Droit.GererLeReferentiel));

        return routes;
    }
}
