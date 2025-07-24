using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    public partial class RemoveFellingSpeciesPercentAndVolumeAndAddVolumeToFellingDetail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Percentage",
                schema: "FellingLicenceApplications",
                table: "FellingSpecies");

            migrationBuilder.DropColumn(
                name: "Volume",
                schema: "FellingLicenceApplications",
                table: "FellingSpecies");

            migrationBuilder.DropColumn(
                name: "Percentage",
                schema: "FellingLicenceApplications",
                table: "ConfirmedFellingSpecies");

            migrationBuilder.DropColumn(
                name: "Volume",
                schema: "FellingLicenceApplications",
                table: "ConfirmedFellingSpecies");

            migrationBuilder.AddColumn<double>(
                name: "EstimatedTotalFellingVolume",
                schema: "FellingLicenceApplications",
                table: "ProposedFellingDetail",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "EstimatedTotalFellingVolume",
                schema: "FellingLicenceApplications",
                table: "ConfirmedFellingDetail",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedTotalFellingVolume",
                schema: "FellingLicenceApplications",
                table: "ProposedFellingDetail");

            migrationBuilder.DropColumn(
                name: "EstimatedTotalFellingVolume",
                schema: "FellingLicenceApplications",
                table: "ConfirmedFellingDetail");

            migrationBuilder.AddColumn<double>(
                name: "Percentage",
                schema: "FellingLicenceApplications",
                table: "FellingSpecies",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Volume",
                schema: "FellingLicenceApplications",
                table: "FellingSpecies",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Percentage",
                schema: "FellingLicenceApplications",
                table: "ConfirmedFellingSpecies",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Volume",
                schema: "FellingLicenceApplications",
                table: "ConfirmedFellingSpecies",
                type: "double precision",
                nullable: true);
        }
    }
}
