using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Assistant.Queries;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Assistant;

public class AskAssistantQueryHandlerTests
{
    private readonly IDocumentRepository documents = Substitute.For<IDocumentRepository>();

    private AskAssistantQueryHandler CreateHandler() => new(documents);

    private static Document CreateIndexedDocument(string content)
    {
        var document = new Document("DOC-NAVIS-GUIDE", "Guide Navis N4 — Bridge", DocumentSourceCategory.NavisGuide, "3.8.25", content);
        document.SubmitForValidation();
        document.Validate();
        document.Activate();
        return document;
    }

    [Fact]
    public async Task Handle_NoIndexedDocuments_ReturnsInsufficientSource()
    {
        documents.ListIndexedAsync(Arg.Any<CancellationToken>()).Returns([]);
        var handler = CreateHandler();

        var answer = await handler.Handle(new AskAssistantQuery("Comment redémarrer le Bridge ?"), CancellationToken.None);

        answer.HasSufficientSource.Should().BeFalse();
        answer.Sources.Should().BeEmpty();
        answer.InsufficientSourceRecommendation.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_MatchingIndexedDocument_ReturnsSourcedAnswerWithLineCitation()
    {
        var document = CreateIndexedDocument("Ligne sans intérêt\nPour redémarrer le Bridge, arrêter d'abord le Center Node.");
        documents.ListIndexedAsync(Arg.Any<CancellationToken>()).Returns([document]);
        var handler = CreateHandler();

        var answer = await handler.Handle(new AskAssistantQuery("Comment redémarrer le Bridge ?"), CancellationToken.None);

        answer.HasSufficientSource.Should().BeTrue();
        var source = answer.Sources.Should().ContainSingle().Subject;
        source.DocumentKey.Should().Be("DOC-NAVIS-GUIDE");
        source.MatchedLineNumber.Should().Be(2);
    }

    [Fact]
    public async Task Handle_QuestionWithoutExploitableTerms_ReturnsInsufficientSourceWithoutQueryingRepository()
    {
        var handler = CreateHandler();

        var answer = await handler.Handle(new AskAssistantQuery("?? ?"), CancellationToken.None);

        answer.HasSufficientSource.Should().BeFalse();
        await documents.DidNotReceive().ListIndexedAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Handler_HasNoMutatingDependency_StructuralGuardRailFR084()
    {
        var constructor = typeof(AskAssistantQueryHandler).GetConstructors().Single();
        var parameterTypes = constructor.GetParameters().Select(p => p.ParameterType).ToList();

        parameterTypes.Should().NotContain(typeof(IUnitOfWork));
        parameterTypes.Should().OnlyContain(t => t == typeof(IDocumentRepository));
    }
}
