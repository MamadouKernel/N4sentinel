using FluentAssertions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using Xunit;

namespace N4Sentinel.Domain.Tests.Entities;

public class SopAssociationTests
{
    [Fact]
    public void Constructor_WithNeitherIncidentNorOperation_Throws()
    {
        var act = () => new SopAssociation(
            Guid.NewGuid(), 1, diagnosticCaseId: null, operationRunId: null, null, null, null, null, "operateur1");

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Constructor_WithEmptyAttachedByUserId_Throws()
    {
        var act = () => new SopAssociation(
            Guid.NewGuid(), 1, Guid.NewGuid(), null, null, null, null, null, "");

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Constructor_WithIncidentOnly_Succeeds()
    {
        var diagnosticCaseId = Guid.NewGuid();

        var association = new SopAssociation(
            Guid.NewGuid(), 2, diagnosticCaseId, null, "Bridge", "Timeout de connexion", "Résolu",
            "Capture des logs post-redémarrage", "operateur1");

        association.DiagnosticCaseId.Should().Be(diagnosticCaseId);
        association.OperationRunId.Should().BeNull();
        association.SopVersionNumber.Should().Be(2);
    }

    [Fact]
    public void Constructor_WithOperationOnly_Succeeds()
    {
        var operationRunId = Guid.NewGuid();

        var association = new SopAssociation(
            Guid.NewGuid(), 1, null, operationRunId, null, null, null, null, "operateur1");

        association.OperationRunId.Should().Be(operationRunId);
        association.DiagnosticCaseId.Should().BeNull();
    }
}
