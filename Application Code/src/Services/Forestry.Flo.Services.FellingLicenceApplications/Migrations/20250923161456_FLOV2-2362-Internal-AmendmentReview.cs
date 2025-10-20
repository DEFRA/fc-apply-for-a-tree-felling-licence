using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    [ExcludeFromCodeCoverage]
    /// <inheritdoc />
    public partial class FLOV22362InternalAmendmentReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AmendmentReviewCompleted",
                schema: "FellingLicenceApplications",
                table: "FellingAndRestockingAmendmentReview",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AmendmentsReason",
                schema: "FellingLicenceApplications",
                table: "FellingAndRestockingAmendmentReview",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmendmentReviewCompleted",
                schema: "FellingLicenceApplications",
                table: "FellingAndRestockingAmendmentReview");

            migrationBuilder.DropColumn(
                name: "AmendmentsReason",
                schema: "FellingLicenceApplications",
                table: "FellingAndRestockingAmendmentReview");
        }
    }
}
