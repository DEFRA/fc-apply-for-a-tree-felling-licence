using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class FLOV22527StoretheIDofthewoodlandofficerapplicant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AmendingWoodlandOfficerId",
                schema: "FellingLicenceApplications",
                table: "FellingAndRestockingAmendmentReview",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RespondingApplicantId",
                schema: "FellingLicenceApplications",
                table: "FellingAndRestockingAmendmentReview",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmendingWoodlandOfficerId",
                schema: "FellingLicenceApplications",
                table: "FellingAndRestockingAmendmentReview");

            migrationBuilder.DropColumn(
                name: "RespondingApplicantId",
                schema: "FellingLicenceApplications",
                table: "FellingAndRestockingAmendmentReview");
        }
    }
}
