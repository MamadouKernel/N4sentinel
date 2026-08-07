using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace N4Sentinel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSopsAndReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SopAssociations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SopVersionNumber = table.Column<int>(type: "int", nullable: false),
                    DiagnosticCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OperationRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ComponentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Result = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Evidence = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AttachedByUserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AttachedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SopAssociations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SopExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SopVersionNumber = table.Column<int>(type: "int", nullable: false),
                    StartedByUserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ResolvedIssue = table.Column<bool>(type: "bit", nullable: true),
                    AbortReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SopExecutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SopKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Objective = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Prerequisites = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StepsText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Controls = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Risks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RollbackPlan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    N4Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsGeneratedFromExecution = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sops", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SopExecutionStepConfirmations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SopExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepIndex = table.Column<int>(type: "int", nullable: false),
                    StepText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConfirmedByUserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Proof = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DeviationComment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SopExecutionStepConfirmations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SopExecutionStepConfirmations_SopExecutions_SopExecutionId",
                        column: x => x.SopExecutionId,
                        principalTable: "SopExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SopAssociations_DiagnosticCaseId",
                table: "SopAssociations",
                column: "DiagnosticCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_SopAssociations_OperationRunId",
                table: "SopAssociations",
                column: "OperationRunId");

            migrationBuilder.CreateIndex(
                name: "IX_SopAssociations_SopId",
                table: "SopAssociations",
                column: "SopId");

            migrationBuilder.CreateIndex(
                name: "IX_SopExecutions_SopId",
                table: "SopExecutions",
                column: "SopId");

            migrationBuilder.CreateIndex(
                name: "IX_SopExecutionStepConfirmations_SopExecutionId",
                table: "SopExecutionStepConfirmations",
                column: "SopExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_Sops_SopKey_VersionNumber",
                table: "Sops",
                columns: new[] { "SopKey", "VersionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SopAssociations");

            migrationBuilder.DropTable(
                name: "SopExecutionStepConfirmations");

            migrationBuilder.DropTable(
                name: "Sops");

            migrationBuilder.DropTable(
                name: "SopExecutions");
        }
    }
}
