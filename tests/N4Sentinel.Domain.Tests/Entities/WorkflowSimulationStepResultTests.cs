using FluentAssertions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using Xunit;

namespace N4Sentinel.Domain.Tests.Entities;

public class WorkflowSimulationStepResultTests
{
    [Fact]
    public void Constructor_WithEmptyName_Throws()
    {
        var act = () => new WorkflowSimulationStepResult(
            Guid.NewGuid(), 0, "", WorkflowStepAction.Start, null, null, null, true, null, false, false, false, null);

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Constructor_WithoutComponent_HasNullObservedHealth()
    {
        var step = new WorkflowSimulationStepResult(
            Guid.NewGuid(), 0, "Purge job", WorkflowStepAction.Custom, null, null, null, true, null,
            false, false, false, null);

        step.ComponentId.Should().BeNull();
        step.ObservedHealth.Should().BeNull();
    }
}
