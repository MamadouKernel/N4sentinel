using FluentAssertions;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Common;
using N4Sentinel.Application.Common.Behaviors;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Common;

public class AuditBehaviorTests
{
    private readonly IAuditEntryRepository auditEntries = Substitute.For<IAuditEntryRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private sealed record AuditableCommand(string Actor) : IRequest, IAuditableRequest
    {
        string IAuditableRequest.ActorUserId => Actor;
        string IAuditableRequest.Action => "Action de test";
        string IAuditableRequest.Summary => "Résumé de test";
    }

    private sealed record NonAuditableCommand : IRequest;

    private AuditBehavior<TRequest, Unit> CreateBehavior<TRequest>() where TRequest : notnull =>
        new(auditEntries, unitOfWork);

    [Fact]
    public async Task Handle_AuditableRequest_WritesEntryAfterSuccess()
    {
        var behavior = CreateBehavior<AuditableCommand>();
        var request = new AuditableCommand("operateur@n4sentinel.local");

        await behavior.Handle(request, (_) => Task.FromResult(Unit.Value), CancellationToken.None);

        auditEntries.Received(1).Add(Arg.Is<AuditEntry>(e =>
            e!.ActorUserId == "operateur@n4sentinel.local" &&
            e.Action == "Action de test" &&
            e.Succeeded));
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonAuditableRequest_DoesNotWriteEntry()
    {
        var behavior = CreateBehavior<NonAuditableCommand>();

        await behavior.Handle(new NonAuditableCommand(), (_) => Task.FromResult(Unit.Value), CancellationToken.None);

        auditEntries.DidNotReceiveWithAnyArgs().Add(default!);
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_HandlerThrows_DoesNotWriteEntry()
    {
        var behavior = CreateBehavior<AuditableCommand>();
        var request = new AuditableCommand("operateur@n4sentinel.local");

        var act = () => behavior.Handle(
            request, (_) => throw new InvalidOperationException("échec du handler"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        auditEntries.DidNotReceiveWithAnyArgs().Add(default!);
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }
}
