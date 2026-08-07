using FluentValidation;
using MediatR;

namespace N4Sentinel.Application.Common.Behaviors;

/// <summary>
/// Exécute tous les FluentValidation validators enregistrés pour la requête avant d'atteindre le handler.
/// Centralise la règle "aucune opération mutative n'est lancée si une information obligatoire est absente"
/// (cf. FR-011 et, plus généralement, tout le cahier des charges).
/// </summary>
/// <remarks>
/// Contrainte volontairement <c>notnull</c> et non <c>IRequest&lt;TResponse&gt;</c> : dans MediatR 14 (via
/// MediatR.Contracts 2.0.1), <c>IRequest</c> (sans réponse) n'hérite plus de <c>IRequest&lt;Unit&gt;</c> — ce
/// sont deux interfaces sœurs distinctes. Une contrainte <c>where TRequest : IRequest&lt;TResponse&gt;</c>
/// empêche silencieusement .NET de construire ce comportement pour toute commande <c>: IRequest</c> pure (la
/// plupart des commandes de mutation de cette base de code), désactivant la validation pour elles sans la
/// moindre erreur ni avertissement — bug découvert et corrigé au Sprint 9 en déboguant l'audit (E10.1).
/// </remarks>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var failures = (await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken))))
                .SelectMany(result => result.Errors)
                .Where(failure => failure is not null)
                .ToList();

            if (failures.Count != 0)
            {
                throw new ValidationException(failures);
            }
        }

        return await next(cancellationToken);
    }
}
