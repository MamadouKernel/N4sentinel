using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace N4Sentinel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLogFileCorrelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiagnosticCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnvironmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Symptom = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    PeriodStartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEndUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CorrelationReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RequestedByUserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConclusionLevel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ConclusionSummary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ConcludedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiagnosticCases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportedLogFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnvironmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CorrelationReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RetentionDays = table.Column<int>(type: "int", nullable: true),
                    AnalysisStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ImportedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AnalyzedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalLineCount = table.Column<int>(type: "int", nullable: false),
                    ErrorLineCount = table.Column<int>(type: "int", nullable: false),
                    WarningLineCount = table.Column<int>(type: "int", nullable: false),
                    DetectedSignatures = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Verdict = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedLogFiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiagnosticHypotheses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiagnosticCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AppliedRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AppliedRuleKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AppliedRuleVersion = table.Column<int>(type: "int", nullable: true),
                    CauseDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ConfidenceLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SupportingEvidence = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ContradictingEvidence = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MissingInformation = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RecommendedChecks = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SafeActionsOrEscalation = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiagnosticHypotheses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiagnosticHypotheses_DiagnosticCases_DiagnosticCaseId",
                        column: x => x.DiagnosticCaseId,
                        principalTable: "DiagnosticCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticCases_CorrelationReference",
                table: "DiagnosticCases",
                column: "CorrelationReference");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticCases_EnvironmentId",
                table: "DiagnosticCases",
                column: "EnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticHypotheses_DiagnosticCaseId",
                table: "DiagnosticHypotheses",
                column: "DiagnosticCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedLogFiles_CorrelationReference",
                table: "ImportedLogFiles",
                column: "CorrelationReference");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedLogFiles_EnvironmentId",
                table: "ImportedLogFiles",
                column: "EnvironmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiagnosticHypotheses");

            migrationBuilder.DropTable(
                name: "ImportedLogFiles");

            migrationBuilder.DropTable(
                name: "DiagnosticCases");
        }
    }
}
