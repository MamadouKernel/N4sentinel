using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace N4Sentinel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedFoldersAndSyncEndpoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SharedFolders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnvironmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsAccessible = table.Column<bool>(type: "bit", nullable: false),
                    UsedCapacityPercent = table.Column<int>(type: "int", nullable: true),
                    StructureValid = table.Column<bool>(type: "bit", nullable: false),
                    CorruptionStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AnomalyDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LastCheckedUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedFolders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncEndpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnvironmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    QueueSize = table.Column<int>(type: "int", nullable: true),
                    ConsumerCount = table.Column<int>(type: "int", nullable: true),
                    LastNormalExchangeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AnomalyDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LastCheckedUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncEndpoints", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SharedFolders_EnvironmentId_Name",
                table: "SharedFolders",
                columns: new[] { "EnvironmentId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncEndpoints_EnvironmentId_Name",
                table: "SyncEndpoints",
                columns: new[] { "EnvironmentId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SharedFolders");

            migrationBuilder.DropTable(
                name: "SyncEndpoints");
        }
    }
}
