using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    public partial class ExpandWoodlandOfficerChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ComplianceRecommendationsEnacted",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EpsLicenceConsidered",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "InterestDeclarationCompleted",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "InterestDeclared",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LocalAuthorityConsulted",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MapAccuracyConfirmed",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Stage1HabitatRegulationsAssessmentRequired",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ComplianceRecommendationsEnacted",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview");

            migrationBuilder.DropColumn(
                name: "EpsLicenceConsidered",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview");

            migrationBuilder.DropColumn(
                name: "InterestDeclarationCompleted",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview");

            migrationBuilder.DropColumn(
                name: "InterestDeclared",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview");

            migrationBuilder.DropColumn(
                name: "LocalAuthorityConsulted",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview");

            migrationBuilder.DropColumn(
                name: "MapAccuracyConfirmed",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview");

            migrationBuilder.DropColumn(
                name: "Stage1HabitatRegulationsAssessmentRequired",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview");
        }
    }
}
