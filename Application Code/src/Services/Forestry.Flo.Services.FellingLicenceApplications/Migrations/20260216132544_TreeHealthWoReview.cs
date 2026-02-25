using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    public partial class TreeHealthWoReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsApplicantTreeHealthAnswersConfirmed",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview",
                newName: "IsTreeHealthReasonToExpedite");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsTreeHealthReasonToExpedite",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview",
                newName: "IsApplicantTreeHealthAnswersConfirmed");
        }
    }
}
