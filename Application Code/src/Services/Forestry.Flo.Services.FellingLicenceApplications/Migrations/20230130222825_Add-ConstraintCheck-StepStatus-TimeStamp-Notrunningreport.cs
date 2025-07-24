using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    public partial class AddConstraintCheckStepStatusTimeStampNotrunningreport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ConstraintCheckStatus",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplicationStepStatus",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExternalLisAccessedTimestamp",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplication",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotRunningExternalLisReport",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplication",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConstraintCheckStatus",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplicationStepStatus");

            migrationBuilder.DropColumn(
                name: "ExternalLisAccessedTimestamp",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplication");

            migrationBuilder.DropColumn(
                name: "NotRunningExternalLisReport",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplication");
        }
    }
}
