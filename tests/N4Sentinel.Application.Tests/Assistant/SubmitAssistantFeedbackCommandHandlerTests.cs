using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Assistant.Commands;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Assistant;

public class SubmitAssistantFeedbackCommandHandlerTests
{
    private readonly IDocumentRepository documents = Substitute.For<IDocumentRepository>();
    private readonly IAssistantFeedbackRepository feedback = Substitute.For<IAssistantFeedbackRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private SubmitAssistantFeedbackCommandHandler CreateHandler() => new(documents, feedback, unitOfWork);

    [Fact]
    public async Task Handle_UnknownDocument_ThrowsKeyNotFoundException()
    {
        documents.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Document?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new SubmitAssistantFeedbackCommand(Guid.NewGuid(), "Question", null, "Correction", "operateur@n4sentinel.local"),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_KnownDocument_CreatesFeedbackAndSaves()
    {
        var document = new Document("DOC-NAVIS-GUIDE", "Titre", DocumentSourceCategory.NavisGuide, null, "Contenu");
        documents.GetByIdAsync(document.Id, Arg.Any<CancellationToken>()).Returns(document);
        var handler = CreateHandler();

        var id = await handler.Handle(
            new SubmitAssistantFeedbackCommand(
                document.Id, "Comment redémarrer le Bridge ?", "Redémarrer le Bridge en premier",
                "Il faut redémarrer le Center Node avant le Bridge.", "operateur@n4sentinel.local"),
            CancellationToken.None);

        id.Should().NotBeEmpty();
        feedback.Received(1).Add(Arg.Is<AssistantFeedback>(f => f!.DocumentId == document.Id && f.Status == FeedbackStatus.Pending));
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
