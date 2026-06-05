using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    [ExcludeFromCodeCoverage]
    /// <inheritdoc />
    public partial class FLOV22632LicenceExpiryDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LicenceExpiryDate",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplication",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LicenceExpiryDate",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplication");
        }
    }
}
