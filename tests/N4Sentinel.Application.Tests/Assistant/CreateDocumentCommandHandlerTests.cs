using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Assistant.Commands;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Assistant;

public class CreateDocumentCommandHandlerTests
{
    private readonly IDocumentRepository documents = Substitute.For<IDocumentRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private CreateDocumentCommandHandler CreateHandler() => new(documents, unitOfWork);

    private static CreateDocumentCommand ValidCommand(string documentKey = "DOC-NAVIS-GUIDE") => new(
        documentKey, "Guide Navis N4 3.8.25 — Cluster Services", DocumentSourceCategory.NavisGuide, "3.8.25",
        "Le Cluster Node passe par les statuts LOADING, WAITING, ACTIVE.");

    [Fact]
    public async Task Handle_DuplicateDocumentKey_ThrowsDomainRuleException()
    {
        var existing = new Document(
            "DOC-NAVIS-GUIDE", "Titre", DocumentSourceCategory.NavisGuide, null, "Contenu");
        documents.ListByDocumentKeyAsync("DOC-NAVIS-GUIDE", Arg.Any<CancellationToken>()).Returns([existing]);
        var handler = CreateHandler();

        var act = () => handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainRuleException>();
    }

    [Fact]
    public async Task Handle_NewDocumentKey_CreatesDraftVersion1AndSaves()
    {
        documents.ListByDocumentKeyAsync("DOC-NAVIS-GUIDE", Arg.Any<CancellationToken>()).Returns([]);
        var handler = CreateHandler();

        var id = await handler.Handle(ValidCommand(), CancellationToken.None);

        id.Should().NotBeEmpty();
        documents.Received(1).Add(Arg.Is<Document>(d =>
            d!.DocumentKey == "DOC-NAVIS-GUIDE" && d.VersionNumber == 1 && d.Status == DocumentStatus.Draft));
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
