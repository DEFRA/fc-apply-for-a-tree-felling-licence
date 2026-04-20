using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    public partial class ReferAndRefuseReasonsOnApproverReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationRefusedReason",
                schema: "FellingLicenceApplications",
                table: "ApproverReview",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferToLocalAuthorityReason",
                schema: "FellingLicenceApplications",
                table: "ApproverReview",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicationRefusedReason",
                schema: "FellingLicenceApplications",
                table: "ApproverReview");

            migrationBuilder.DropColumn(
                name: "ReferToLocalAuthorityReason",
                schema: "FellingLicenceApplications",
                table: "ApproverReview");
        }
    }
}
