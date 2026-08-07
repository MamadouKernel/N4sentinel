using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Sops.Commands;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Sops;

public class CreateSopCommandHandlerTests
{
    private readonly ISopRepository sops = Substitute.For<ISopRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private CreateSopCommandHandler CreateHandler() => new(sops, unitOfWork);

    private static CreateSopCommand ValidCommand(string sopKey = "SOP-CLUSTER-RESTART") => new(
        sopKey, "Redémarrage contrôlé d'un Cluster Node", "Rétablir un Cluster Node en échec",
        "Aucune opération critique en cours", "Arrêter le service\nVérifier les logs\nRedémarrer le service",
        "Le service répond sur le port 8080", "Perte de connexions actives", "Restaurer depuis la sauvegarde", "3.8.25");

    [Fact]
    public async Task Handle_DuplicateSopKey_ThrowsDomainRuleException()
    {
        var existing = new Sop("SOP-CLUSTER-RESTART", "Titre", "Objectif", null, "Étape 1", null, null, null, null);
        sops.ListBySopKeyAsync("SOP-CLUSTER-RESTART", Arg.Any<CancellationToken>()).Returns([existing]);
        var handler = CreateHandler();

        var act = () => handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainRuleException>();
    }

    [Fact]
    public async Task Handle_NewSopKey_CreatesDraftVersion1AndSaves()
    {
        sops.ListBySopKeyAsync("SOP-CLUSTER-RESTART", Arg.Any<CancellationToken>()).Returns([]);
        var handler = CreateHandler();

        var id = await handler.Handle(ValidCommand(), CancellationToken.None);

        id.Should().NotBeEmpty();
        sops.Received(1).Add(Arg.Is<Sop>(s =>
            s!.SopKey == "SOP-CLUSTER-RESTART" && s.VersionNumber == 1 && s.Status == SopStatus.Draft));
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
