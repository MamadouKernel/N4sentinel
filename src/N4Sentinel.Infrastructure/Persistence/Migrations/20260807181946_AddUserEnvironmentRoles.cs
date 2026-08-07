using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace N4Sentinel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserEnvironmentRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserEnvironmentRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    EnvironmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GrantedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    GrantedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEnvironmentRoles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserEnvironmentRoles_EnvironmentId",
                table: "UserEnvironmentRoles",
                column: "EnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserEnvironmentRoles_UserId_EnvironmentId",
                table: "UserEnvironmentRoles",
                columns: new[] { "UserId", "EnvironmentId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserEnvironmentRoles");
        }
    }
}
