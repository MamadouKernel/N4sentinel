using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace N4Sentinel.Data.Migrations
{
    /// <inheritdoc />
    public partial class Sprint7_ExecutionReelle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Contournable",
                table: "EtapesDeWorkflow",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Contournable",
                table: "EtapesDeWorkflow");
        }
    }
}
