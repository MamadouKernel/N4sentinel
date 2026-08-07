using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace N4Sentinel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowSimulations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkflowSimulations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowVersionNumber = table.Column<int>(type: "int", nullable: false),
                    EnvironmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByUserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowSimulations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowSimulationStepResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ComponentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ObservedHealth = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CanExecute = table.Column<bool>(type: "bit", nullable: false),
                    BlockingReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequiresConfirmation = table.Column<bool>(type: "bit", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false),
                    IsCriticalOrDestructive = table.Column<bool>(type: "bit", nullable: false),
                    ExpectedDurationSeconds = table.Column<int>(type: "int", nullable: true),
                    WorkflowSimulationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowSimulationStepResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowSimulationStepResults_WorkflowSimulations_WorkflowSimulationId",
                        column: x => x.WorkflowSimulationId,
                        principalTable: "WorkflowSimulations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSimulations_WorkflowId",
                table: "WorkflowSimulations",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSimulationStepResults_WorkflowSimulationId",
                table: "WorkflowSimulationStepResults",
                column: "WorkflowSimulationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowSimulationStepResults");

            migrationBuilder.DropTable(
                name: "WorkflowSimulations");
        }
    }
}
