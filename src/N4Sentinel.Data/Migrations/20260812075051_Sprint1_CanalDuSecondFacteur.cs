using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace N4Sentinel.Data.Migrations
{
    /// <inheritdoc />
    public partial class Sprint1_CanalDuSecondFacteur : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MethodeDeSecondFacteur",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MethodeDeSecondFacteur",
                table: "AspNetUsers");
        }
    }
}
