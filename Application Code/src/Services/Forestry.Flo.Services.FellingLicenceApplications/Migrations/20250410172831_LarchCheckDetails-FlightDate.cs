using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    [ExcludeFromCodeCoverage]
    /// <inheritdoc />
    public partial class LarchCheckDetailsFlightDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "FlightDate",
                schema: "FellingLicenceApplications",
                table: "LarchCheckDetails",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FlightObservations",
                schema: "FellingLicenceApplications",
                table: "LarchCheckDetails",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FlightDate",
                schema: "FellingLicenceApplications",
                table: "LarchCheckDetails");

            migrationBuilder.DropColumn(
                name: "FlightObservations",
                schema: "FellingLicenceApplications",
                table: "LarchCheckDetails");
        }
    }
}
