using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace N4Sentinel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSequenceExceptionToWorkflowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SequenceExceptionApprovedAtUtc",
                table: "WorkflowVersions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SequenceExceptionApprovedByUserId",
                table: "WorkflowVersions",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SequenceExceptionReason",
                table: "WorkflowVersions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SequenceExceptionApprovedAtUtc",
                table: "WorkflowVersions");

            migrationBuilder.DropColumn(
                name: "SequenceExceptionApprovedByUserId",
                table: "WorkflowVersions");

            migrationBuilder.DropColumn(
                name: "SequenceExceptionReason",
                table: "WorkflowVersions");
        }
    }
}
