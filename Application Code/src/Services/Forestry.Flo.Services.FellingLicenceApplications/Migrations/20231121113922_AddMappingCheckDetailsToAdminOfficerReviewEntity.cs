using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class AddMappingCheckDetailsToAdminOfficerReviewEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MappingCheckFailureReason",
                schema: "FellingLicenceApplications",
                table: "AdminOfficerReview",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MappingCheckPassed",
                schema: "FellingLicenceApplications",
                table: "AdminOfficerReview",
                type: "boolean",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MappingCheckFailureReason",
                schema: "FellingLicenceApplications",
                table: "AdminOfficerReview");

            migrationBuilder.DropColumn(
                name: "MappingCheckPassed",
                schema: "FellingLicenceApplications",
                table: "AdminOfficerReview");
        }
    }
}
