using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Assistant.Queries;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Assistant;

public class SearchDocumentsQueryHandlerTests
{
    private readonly IDocumentRepository documents = Substitute.For<IDocumentRepository>();

    private SearchDocumentsQueryHandler CreateHandler() => new(documents);

    [Fact]
    public async Task Handle_EmptyFreeText_ReturnsEmptyWithoutQueryingRepository()
    {
        var handler = CreateHandler();

        var results = await handler.Handle(new SearchDocumentsQuery(""), CancellationToken.None);

        results.Should().BeEmpty();
        await documents.DidNotReceive().ListIndexedAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MatchingIndexedDocument_ReturnsResultWithLineNumber()
    {
        var document = new Document(
            "DOC-PROC-BRIDGE", "Procédure Bridge", DocumentSourceCategory.InternalProcedure, null,
            "Étape 1 : vérifier le socket.\nÉtape 2 : timeout Bridge observé, escalader.");
        document.SubmitForValidation();
        document.Validate();
        document.Activate();
        documents.ListIndexedAsync(Arg.Any<CancellationToken>()).Returns([document]);
        var handler = CreateHandler();

        var results = await handler.Handle(new SearchDocumentsQuery("timeout"), CancellationToken.None);

        results.Should().ContainSingle().Which.MatchedLineNumber.Should().Be(2);
    }
}
