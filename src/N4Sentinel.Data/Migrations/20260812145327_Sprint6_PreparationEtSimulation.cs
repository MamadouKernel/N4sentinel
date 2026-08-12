using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace N4Sentinel.Data.Migrations
{
    /// <inheritdoc />
    public partial class Sprint6_PreparationEtSimulation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FenetreDebut",
                table: "Executions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FenetreFin",
                table: "Executions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImpactAttendu",
                table: "Executions",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Perimetre",
                table: "Executions",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatutDuPreCheck",
                table: "EtapesDExecution",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ApprobationsDExecution",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprouvePar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Decision = table.Column<int>(type: "int", nullable: false),
                    DecideLe = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Motif = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprobationsDExecution", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprobationsDExecution_Executions_ExecutionId",
                        column: x => x.ExecutionId,
                        principalTable: "Executions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Workflows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnvironmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nom = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workflows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VersionsDeWorkflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroDeVersion = table.Column<int>(type: "int", nullable: false),
                    Statut = table.Column<int>(type: "int", nullable: false),
                    CommentaireDeVersion = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    CreeLe = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreePar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ValideLe = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ValidePar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ActionSensible = table.Column<bool>(type: "bit", nullable: false),
                    Circuit = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VersionsDeWorkflow", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VersionsDeWorkflow_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EtapesDeWorkflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ComposantCibleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Condition = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    TimeoutSecondes = table.Column<int>(type: "int", nullable: false),
                    NombreDeReessais = table.Column<int>(type: "int", nullable: false),
                    DelaiEntreReessaisSecondes = table.Column<int>(type: "int", nullable: false),
                    ConfirmationRequise = table.Column<bool>(type: "bit", nullable: false),
                    ApprobationRequise = table.Column<bool>(type: "bit", nullable: false),
                    IndependanteDesEtapesVoisines = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EtapesDeWorkflow", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EtapesDeWorkflow_VersionsDeWorkflow_WorkflowVersionId",
                        column: x => x.WorkflowVersionId,
                        principalTable: "VersionsDeWorkflow",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprobationsDExecution_ExecutionId_ApprouvePar",
                table: "ApprobationsDExecution",
                columns: new[] { "ExecutionId", "ApprouvePar" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EtapesDeWorkflow_WorkflowVersionId_Ordre",
                table: "EtapesDeWorkflow",
                columns: new[] { "WorkflowVersionId", "Ordre" });

            migrationBuilder.CreateIndex(
                name: "IX_VersionsDeWorkflow_WorkflowId_NumeroDeVersion",
                table: "VersionsDeWorkflow",
                columns: new[] { "WorkflowId", "NumeroDeVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_EnvironmentId_Nom",
                table: "Workflows",
                columns: new[] { "EnvironmentId", "Nom" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprobationsDExecution");

            migrationBuilder.DropTable(
                name: "EtapesDeWorkflow");

            migrationBuilder.DropTable(
                name: "VersionsDeWorkflow");

            migrationBuilder.DropTable(
                name: "Workflows");

            migrationBuilder.DropColumn(
                name: "FenetreDebut",
                table: "Executions");

            migrationBuilder.DropColumn(
                name: "FenetreFin",
                table: "Executions");

            migrationBuilder.DropColumn(
                name: "ImpactAttendu",
                table: "Executions");

            migrationBuilder.DropColumn(
                name: "Perimetre",
                table: "Executions");

            migrationBuilder.DropColumn(
                name: "StatutDuPreCheck",
                table: "EtapesDExecution");
        }
    }
}
