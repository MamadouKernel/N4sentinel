using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace N4Sentinel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSequenceTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Cette colonne peut déjà exister : elle est créée au démarrage de l'application par une
            // instruction ALTER TABLE conditionnelle (Program.cs), en dehors du mécanisme de migrations.
            // L'ajout est donc rendu idempotent pour que la migration s'applique dans les deux cas.
            migrationBuilder.Sql(
                "IF NOT EXISTS (SELECT 1 FROM sys.columns " +
                "WHERE object_id = OBJECT_ID(N'Environments') AND name = 'AllowedExecutionMode') " +
                "ALTER TABLE [Environments] ADD [AllowedExecutionMode] int NOT NULL DEFAULT 0;");

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "Components",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "JmxMetricSignals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnvironmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComponentName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetricName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetricValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NumericValue = table.Column<double>(type: "float", nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CollectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JmxMetricSignals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SequenceTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    WorkflowType = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    EnvironmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SequenceTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SequenceTiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SequenceTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    ComponentKind = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Execution = table.Column<int>(type: "int", nullable: false),
                    SuccessCriteria = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsOptional = table.Column<bool>(type: "bit", nullable: false),
                    SettleDelaySeconds = table.Column<int>(type: "int", nullable: true),
                    SourceReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SequenceTiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SequenceTiers_SequenceTemplates_SequenceTemplateId",
                        column: x => x.SequenceTemplateId,
                        principalTable: "SequenceTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SequenceTemplates_TemplateKey_VersionNumber",
                table: "SequenceTemplates",
                columns: new[] { "TemplateKey", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SequenceTiers_SequenceTemplateId_Position",
                table: "SequenceTiers",
                columns: new[] { "SequenceTemplateId", "Position" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JmxMetricSignals");

            migrationBuilder.DropTable(
                name: "SequenceTiers");

            migrationBuilder.DropTable(
                name: "SequenceTemplates");

            migrationBuilder.DropColumn(
                name: "AllowedExecutionMode",
                table: "Environments");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Components");
        }
    }
}
