using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    [ExcludeFromCodeCoverage]
    /// <inheritdoc />
    public partial class WoodlandOfficerReviewTreeHealthCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsApplicantTreeHealthAnswersConfirmed",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsApplicantTreeHealthAnswersConfirmed",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview");
        }
    }
}
