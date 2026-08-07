using FluentAssertions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using Xunit;

namespace N4Sentinel.Domain.Tests.Entities;

public class AssistantFeedbackTests
{
    private static AssistantFeedback CreateFeedback() => new(
        Guid.NewGuid(), "Comment redémarrer le Bridge ?", "Redémarrer le Bridge en premier",
        "Il faut redémarrer le Center Node avant le Bridge, pas l'inverse.", "operateur@n4sentinel.local");

    [Fact]
    public void Constructor_WithEmptyProposedCorrection_Throws()
    {
        var act = () => new AssistantFeedback(Guid.NewGuid(), "Question", null, "", "operateur@n4sentinel.local");

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void NewFeedback_IsPending()
    {
        var feedback = CreateFeedback();

        feedback.Status.Should().Be(FeedbackStatus.Pending);
    }

    [Fact]
    public void Validate_SetsStatusAndReviewer()
    {
        var feedback = CreateFeedback();

        feedback.Validate("admin@n4sentinel.local", "Confirmé exact, à corriger dans la prochaine version.");

        feedback.Status.Should().Be(FeedbackStatus.Validated);
        feedback.ReviewedByUserId.Should().Be("admin@n4sentinel.local");
        feedback.ReviewedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Reject_WithoutReason_Throws()
    {
        var feedback = CreateFeedback();

        var act = () => feedback.Reject("admin@n4sentinel.local", "");

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Validate_AlreadyReviewed_Throws()
    {
        var feedback = CreateFeedback();
        feedback.Validate("admin@n4sentinel.local", null);

        var act = () => feedback.Reject("admin@n4sentinel.local", "trop tard");

        act.Should().Throw<DomainRuleException>();
    }
}
