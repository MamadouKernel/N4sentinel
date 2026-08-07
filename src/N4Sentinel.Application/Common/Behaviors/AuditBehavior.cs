using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Common.Behaviors;

/// <summary>
/// Journalise dans le journal d'audit (E10.1) les commandes qui implémentent <see cref="IAuditableRequest"/>,
/// une fois qu'elles ont réussi. N'audite volontairement que les succès : le comportement établi des handlers
/// de cette base de code est d'appeler <c>SaveChangesAsync</c> en toute dernière instruction — si ce
/// comportement appelait <c>SaveChangesAsync</c> après un échec pour persister une entrée d'audit d'échec, il
/// persisterait aussi silencieusement toute mutation de domaine déjà suivie par le change tracker avant
/// l'exception. Cf. décision de cadrage du Sprint 9 (docs/scrum/sprints/sprint-9.md).
/// </summary>
/// <remarks>
/// Contrainte volontairement <c>notnull</c> et non <c>IRequest&lt;TResponse&gt;</c> : cf. <see cref="ValidationBehavior{TRequest, TResponse}"/>.
/// </remarks>
public sealed class AuditBehavior<TRequest, TResponse>(
    IAuditEntryRepository auditEntries, IUnitOfWork unitOfWork) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);

        if (request is IAuditableRequest auditable)
        {
            auditEntries.Add(new AuditEntry(auditable.ActorUserId, auditable.Action, auditable.Summary, succeeded: true));
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return response;
    }
}
