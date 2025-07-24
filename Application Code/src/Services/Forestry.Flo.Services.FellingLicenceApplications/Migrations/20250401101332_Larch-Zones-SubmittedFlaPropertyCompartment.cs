using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    public partial class LarchZonesSubmittedFlaPropertyCompartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Zone1",
                schema: "FellingLicenceApplications",
                table: "SubmittedFlaPropertyCompartment",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Zone2",
                schema: "FellingLicenceApplications",
                table: "SubmittedFlaPropertyCompartment",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Zone3",
                schema: "FellingLicenceApplications",
                table: "SubmittedFlaPropertyCompartment",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Zone1",
                schema: "FellingLicenceApplications",
                table: "SubmittedFlaPropertyCompartment");

            migrationBuilder.DropColumn(
                name: "Zone2",
                schema: "FellingLicenceApplications",
                table: "SubmittedFlaPropertyCompartment");

            migrationBuilder.DropColumn(
                name: "Zone3",
                schema: "FellingLicenceApplications",
                table: "SubmittedFlaPropertyCompartment");
        }
    }
}
