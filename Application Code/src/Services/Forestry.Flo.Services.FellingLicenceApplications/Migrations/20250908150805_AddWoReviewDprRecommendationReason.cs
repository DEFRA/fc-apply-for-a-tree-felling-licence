using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    public partial class AddWoReviewDprRecommendationReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RecommendationForDecisionPublicRegisterReason",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecommendationForDecisionPublicRegisterReason",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview");
        }
    }
}
