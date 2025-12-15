using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    public partial class FLOV22632ApprovedInErrorLicenceExpiryDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CaseNote",
                schema: "FellingLicenceApplications",
                table: "ApprovedInError",
                newName: "SupplementaryPointsText");

            migrationBuilder.AddColumn<DateTime>(
                name: "LicenceExpiryDate",
                schema: "FellingLicenceApplications",
                table: "ApprovedInError",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReasonExpiryDateText",
                schema: "FellingLicenceApplications",
                table: "ApprovedInError",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReasonOtherText",
                schema: "FellingLicenceApplications",
                table: "ApprovedInError",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LicenceExpiryDate",
                schema: "FellingLicenceApplications",
                table: "ApprovedInError");

            migrationBuilder.DropColumn(
                name: "ReasonExpiryDateText",
                schema: "FellingLicenceApplications",
                table: "ApprovedInError");

            migrationBuilder.DropColumn(
                name: "ReasonOtherText",
                schema: "FellingLicenceApplications",
                table: "ApprovedInError");

            migrationBuilder.RenameColumn(
                name: "SupplementaryPointsText",
                schema: "FellingLicenceApplications",
                table: "ApprovedInError",
                newName: "CaseNote");
        }
    }
}
