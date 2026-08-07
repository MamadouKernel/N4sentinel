using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace N4Sentinel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationStepOverride : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OverriddenAtUtc",
                table: "OperationStepExecutions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverriddenByUserId",
                table: "OperationStepExecutions",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverrideAcceptedRisk",
                table: "OperationStepExecutions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverrideApprovedByUserId",
                table: "OperationStepExecutions",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverrideReason",
                table: "OperationStepExecutions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsProductionEnvironment",
                table: "OperationRuns",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OverriddenAtUtc",
                table: "OperationStepExecutions");

            migrationBuilder.DropColumn(
                name: "OverriddenByUserId",
                table: "OperationStepExecutions");

            migrationBuilder.DropColumn(
                name: "OverrideAcceptedRisk",
                table: "OperationStepExecutions");

            migrationBuilder.DropColumn(
                name: "OverrideApprovedByUserId",
                table: "OperationStepExecutions");

            migrationBuilder.DropColumn(
                name: "OverrideReason",
                table: "OperationStepExecutions");

            migrationBuilder.DropColumn(
                name: "IsProductionEnvironment",
                table: "OperationRuns");
        }
    }
}
