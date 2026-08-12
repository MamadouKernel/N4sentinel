using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace N4Sentinel.Data.Migrations
{
    /// <inheritdoc />
    public partial class Sprint2_Referentiel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Composants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnvironmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nom = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Serveur = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AdresseIp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    NomDns = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SystemeDExploitation = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    NomDuService = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Mecanisme = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModeDePilotage = table.Column<int>(type: "int", nullable: false),
                    Responsable = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Criticite = table.Column<int>(type: "int", nullable: false),
                    Sante = table.Column<int>(type: "int", nullable: false),
                    DernierControle = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Statut = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Composants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Composants_Environnements_EnvironmentId",
                        column: x => x.EnvironmentId,
                        principalTable: "Environnements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComposantControles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TypeDeControle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Parametres = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    TimeoutSecondes = table.Column<int>(type: "int", nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComposantControles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComposantControles_Composants_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Composants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComposantDependances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComposantRequisId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Bloquante = table.Column<bool>(type: "bit", nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComposantDependances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComposantDependances_Composants_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Composants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComposantEndpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Protocole = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Hote = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Port = table.Column<int>(type: "int", nullable: true),
                    Chemin = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComposantEndpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComposantEndpoints_Composants_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Composants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComposantControles_ComponentId",
                table: "ComposantControles",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_ComposantDependances_ComponentId_ComposantRequisId",
                table: "ComposantDependances",
                columns: new[] { "ComponentId", "ComposantRequisId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComposantEndpoints_ComponentId",
                table: "ComposantEndpoints",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_Composants_EnvironmentId_Nom",
                table: "Composants",
                columns: new[] { "EnvironmentId", "Nom" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComposantControles");

            migrationBuilder.DropTable(
                name: "ComposantDependances");

            migrationBuilder.DropTable(
                name: "ComposantEndpoints");

            migrationBuilder.DropTable(
                name: "Composants");
        }
    }
}
