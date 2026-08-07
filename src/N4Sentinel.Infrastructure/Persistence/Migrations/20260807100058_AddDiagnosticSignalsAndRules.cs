using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace N4Sentinel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDiagnosticSignalsAndRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiagnosticRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ConditionDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RequiredSources = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Hypothesis = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ConfidenceCalculationMethod = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AdditionalChecks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Recommendation = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiagnosticRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiagnosticSignals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnvironmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ComponentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CorrelationReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsManualImport = table.Column<bool>(type: "bit", nullable: false),
                    CollectionStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UnavailableReason = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    OriginAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reliability = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CollectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiagnosticSignals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticRules_RuleKey_VersionNumber",
                table: "DiagnosticRules",
                columns: new[] { "RuleKey", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticSignals_CorrelationReference",
                table: "DiagnosticSignals",
                column: "CorrelationReference");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticSignals_EnvironmentId",
                table: "DiagnosticSignals",
                column: "EnvironmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiagnosticRules");

            migrationBuilder.DropTable(
                name: "DiagnosticSignals");
        }
    }
}
