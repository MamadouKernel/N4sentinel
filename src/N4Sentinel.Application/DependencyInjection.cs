using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using N4Sentinel.Application.Common.Behaviors;
using N4Sentinel.Application.Operations;

namespace N4Sentinel.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));
        services.AddScoped<OperationStepExecutionService>();
        services.AddScoped<Operations.Queries.CheckOperationPrerequisitesQueryHandler>();
        services.AddTransient<Diagnostics.DiagnosticEngineService>();
        services.AddScoped<Sequences.ISequenceComplianceChecker, Sequences.SequenceComplianceChecker>();

        return services;
    }
}
