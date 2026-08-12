using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace N4Sentinel.Data.Migrations
{
    /// <inheritdoc />
    public partial class Sprint4_Supervision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnMaintenance",
                table: "Composants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Releves",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnvironmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComposantCibleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Domaine = table.Column<int>(type: "int", nullable: false),
                    Cible = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Valeur = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Unite = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    SeuilAttendu = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SeuilDepasse = table.Column<bool>(type: "bit", nullable: false),
                    ReleveLe = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Qualite = table.Column<int>(type: "int", nullable: false),
                    MotifIndisponibilite = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Verdict = table.Column<int>(type: "int", nullable: false),
                    SuffitSeulAConclure = table.Column<bool>(type: "bit", nullable: false),
                    Transition = table.Column<int>(type: "int", nullable: false),
                    ReferenceDeCorrelation = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Releves", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Releves_ComposantCibleId_Type_ReleveLe",
                table: "Releves",
                columns: new[] { "ComposantCibleId", "Type", "ReleveLe" });

            migrationBuilder.CreateIndex(
                name: "IX_Releves_ReleveLe",
                table: "Releves",
                column: "ReleveLe");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Releves");

            migrationBuilder.DropColumn(
                name: "EnMaintenance",
                table: "Composants");
        }
    }
}
