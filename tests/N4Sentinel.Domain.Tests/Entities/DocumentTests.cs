using FluentAssertions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using Xunit;

namespace N4Sentinel.Domain.Tests.Entities;

public class DocumentTests
{
    private static Document CreateDocument() => new(
        "DOC-NAVIS-GUIDE", "Guide Navis N4 3.8.25 — Cluster Services", DocumentSourceCategory.NavisGuide,
        "3.8.25", "Le Cluster Node passe par les statuts LOADING, WAITING, ACTIVE.");

    [Fact]
    public void Constructor_WithEmptyDocumentKey_Throws()
    {
        var act = () => new Document("", "Titre", DocumentSourceCategory.NavisGuide, null, "Contenu");

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void NewDocument_IsDraftVersion1_NotIndexed()
    {
        var document = CreateDocument();

        document.VersionNumber.Should().Be(1);
        document.Status.Should().Be(DocumentStatus.Draft);
        document.IsIndexed.Should().BeFalse();
    }

    [Fact]
    public void FullLifecycle_DraftToActive_IsIndexed()
    {
        var document = CreateDocument();

        document.SubmitForValidation();
        document.Validate();
        document.Activate();

        document.Status.Should().Be(DocumentStatus.Active);
        document.IsIndexed.Should().BeTrue();
    }

    [Fact]
    public void Activate_WithoutValidation_Throws()
    {
        var document = CreateDocument();

        var act = () => document.Activate();

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void UpdateContent_WhenNotDraft_Throws()
    {
        var document = CreateDocument();
        document.SubmitForValidation();

        var act = () => document.UpdateContent("Nouveau titre", DocumentSourceCategory.NavisGuide, null, "Nouveau contenu");

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void CreateNewVersion_SharesDocumentKeyWithIncrementedVersionAsDraft()
    {
        var document = CreateDocument();
        document.SubmitForValidation();
        document.Validate();
        document.Activate();

        var newVersion = document.CreateNewVersion();

        newVersion.DocumentKey.Should().Be(document.DocumentKey);
        newVersion.VersionNumber.Should().Be(2);
        newVersion.Status.Should().Be(DocumentStatus.Draft);
        document.Status.Should().Be(DocumentStatus.Active, "creating a new version must not affect the original");
    }
}
