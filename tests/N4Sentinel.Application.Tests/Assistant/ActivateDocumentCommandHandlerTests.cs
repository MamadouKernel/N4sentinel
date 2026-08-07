using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Assistant.Commands;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Assistant;

public class ActivateDocumentCommandHandlerTests
{
    private readonly IDocumentRepository documents = Substitute.For<IDocumentRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private ActivateDocumentCommandHandler CreateHandler() => new(documents, unitOfWork);

    [Fact]
    public async Task Handle_ActivatingNewVersion_DisablesPreviousActiveSibling()
    {
        var oldVersion = new Document(
            "DOC-NAVIS-GUIDE", "Guide v1", DocumentSourceCategory.NavisGuide, "3.8.25", "Contenu initial");
        oldVersion.SubmitForValidation();
        oldVersion.Validate();
        oldVersion.Activate();

        var newVersion = oldVersion.CreateNewVersion();
        newVersion.SubmitForValidation();
        newVersion.Validate();

        documents.GetByIdAsync(newVersion.Id, Arg.Any<CancellationToken>()).Returns(newVersion);
        documents.ListByDocumentKeyAsync("DOC-NAVIS-GUIDE", Arg.Any<CancellationToken>()).Returns([oldVersion, newVersion]);
        var handler = CreateHandler();

        await handler.Handle(new ActivateDocumentCommand(newVersion.Id), CancellationToken.None);

        newVersion.Status.Should().Be(DocumentStatus.Active);
        oldVersion.Status.Should().Be(DocumentStatus.Disabled);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownDocument_ThrowsKeyNotFoundException()
    {
        documents.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Document?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(new ActivateDocumentCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
