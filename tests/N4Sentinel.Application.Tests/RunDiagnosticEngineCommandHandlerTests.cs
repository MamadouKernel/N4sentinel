using FluentAssertions;
using NSubstitute;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Diagnostics;
using N4Sentinel.Application.Diagnostics.Commands;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using Xunit;

namespace N4Sentinel.Application.Tests;

public class RunDiagnosticEngineCommandHandlerTests
{
    private readonly IDiagnosticCaseRepository _cases = Substitute.For<IDiagnosticCaseRepository>();
    private readonly IDiagnosticSignalRepository _signals = Substitute.For<IDiagnosticSignalRepository>();
    private readonly IDiagnosticRuleRepository _rules = Substitute.For<IDiagnosticRuleRepository>();
    private readonly IImportedLogFileRepository _logFiles = Substitute.For<IImportedLogFileRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RunDiagnosticEngineCommandHandler _sut;

    public RunDiagnosticEngineCommandHandlerTests()
    {
        var engine = new DiagnosticEngineService(_signals, _rules, _logFiles);
        _sut = new RunDiagnosticEngineCommandHandler(_cases, engine, _unitOfWork);
    }

    [Fact]
    public async Task Handle_UnknownCase_ThrowsKeyNotFoundException()
    {
        var command = new RunDiagnosticEngineCommand(Guid.NewGuid(), "admin");
        _cases.GetByIdAsync(command.DiagnosticCaseId, Arg.Any<CancellationToken>()).Returns((DiagnosticCase?)null);

        var act = () => _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_ConcludedCase_ThrowsDomainRuleException()
    {
        var diagnosticCase = new DiagnosticCase(
            Guid.NewGuid(), "Symptom", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, "INC-999", "admin");
        diagnosticCase.Conclude(ConclusionLevel.NoAnomalyDetected, "Synthèse RAS");

        _cases.GetByIdAsync(diagnosticCase.Id, Arg.Any<CancellationToken>()).Returns(diagnosticCase);

        var command = new RunDiagnosticEngineCommand(diagnosticCase.Id, "admin");
        var act = () => _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainRuleException>();
    }

    [Fact]
    public async Task Handle_ValidCase_RunsEngineAndSaves()
    {
        var envId = Guid.NewGuid();
        var caseRef = "INC-888";
        var diagnosticCase = new DiagnosticCase(
            envId, "Lenteur", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, caseRef, "admin");

        _cases.GetByIdAsync(diagnosticCase.Id, Arg.Any<CancellationToken>()).Returns(diagnosticCase);
        _signals.ListByCorrelationAsync(caseRef, Arg.Any<CancellationToken>()).Returns([]);
        _logFiles.ListByCorrelationAsync(caseRef, Arg.Any<CancellationToken>()).Returns([]);
        _rules.ListAllAsync(Arg.Any<CancellationToken>()).Returns([]);

        var command = new RunDiagnosticEngineCommand(diagnosticCase.Id, "admin");
        var count = await _sut.Handle(command, CancellationToken.None);

        count.Should().Be(0);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
