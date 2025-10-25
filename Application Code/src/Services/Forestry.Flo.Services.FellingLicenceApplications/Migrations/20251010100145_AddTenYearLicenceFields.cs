using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddTenYearLicenceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsForTenYearLicence",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplication",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WoodlandManagementPlanReference",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplication",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsForTenYearLicence",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplication");

            migrationBuilder.DropColumn(
                name: "WoodlandManagementPlanReference",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplication");
        }
    }
}
