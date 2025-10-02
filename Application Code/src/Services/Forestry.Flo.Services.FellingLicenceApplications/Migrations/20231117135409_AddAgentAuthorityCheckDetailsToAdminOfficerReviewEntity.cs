using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class AddAgentAuthorityCheckDetailsToAdminOfficerReviewEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AgentAuthorityCheckFailureReason",
                schema: "FellingLicenceApplications",
                table: "AdminOfficerReview",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AgentAuthorityCheckPassed",
                schema: "FellingLicenceApplications",
                table: "AdminOfficerReview",
                type: "boolean",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgentAuthorityCheckFailureReason",
                schema: "FellingLicenceApplications",
                table: "AdminOfficerReview");

            migrationBuilder.DropColumn(
                name: "AgentAuthorityCheckPassed",
                schema: "FellingLicenceApplications",
                table: "AdminOfficerReview");
        }
    }
}
