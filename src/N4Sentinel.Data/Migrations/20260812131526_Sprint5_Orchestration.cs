using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace N4Sentinel.Data.Migrations
{
    /// <inheritdoc />
    public partial class Sprint5_Orchestration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Executions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnvironmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DemandePar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DemandeLe = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Motif = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    ReferenceTicket = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Statut = table.Column<int>(type: "int", nullable: false),
                    ModeSimulation = table.Column<bool>(type: "bit", nullable: false),
                    ApprouvePar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ApprouveLe = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DebutLe = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FinLe = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Resultat = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    ReferenceDeCorrelation = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Executions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VerrousDOperation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnvironmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DetenuPar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AcquisLe = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpireLe = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LibereLe = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LiberePar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerrousDOperation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EtapesDExecution",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowStepDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ComposantCibleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Statut = table.Column<int>(type: "int", nullable: false),
                    DebutLe = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FinLe = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Preuve = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    TypeDErreur = table.Column<int>(type: "int", nullable: false),
                    MessageDErreur = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    NombreDeTentatives = table.Column<int>(type: "int", nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    DecidePar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DecideLe = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    OperateurExecutant = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EtapesDExecution", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EtapesDExecution_Executions_ExecutionId",
                        column: x => x.ExecutionId,
                        principalTable: "Executions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EtapesDExecution_ExecutionId_Ordre",
                table: "EtapesDExecution",
                columns: new[] { "ExecutionId", "Ordre" });

            migrationBuilder.CreateIndex(
                name: "IX_Executions_EnvironmentId_Statut",
                table: "Executions",
                columns: new[] { "EnvironmentId", "Statut" });

            migrationBuilder.CreateIndex(
                name: "IX_Executions_Reference",
                table: "Executions",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerrousDOperation_EnvironmentId_LibereLe_ExpireLe",
                table: "VerrousDOperation",
                columns: new[] { "EnvironmentId", "LibereLe", "ExpireLe" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EtapesDExecution");

            migrationBuilder.DropTable(
                name: "VerrousDOperation");

            migrationBuilder.DropTable(
                name: "Executions");
        }
    }
}
