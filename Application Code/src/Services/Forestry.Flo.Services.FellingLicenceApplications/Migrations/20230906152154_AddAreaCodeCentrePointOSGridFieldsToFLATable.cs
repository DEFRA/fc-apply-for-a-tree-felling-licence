using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class AddAreaCodeCentrePointOSGridFieldsToFLATable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AreaCode",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplication",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CentrePoint",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplication",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OSGridReference",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplication",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AreaCode",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplication");

            migrationBuilder.DropColumn(
                name: "CentrePoint",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplication");

            migrationBuilder.DropColumn(
                name: "OSGridReference",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplication");
        }
    }
}
