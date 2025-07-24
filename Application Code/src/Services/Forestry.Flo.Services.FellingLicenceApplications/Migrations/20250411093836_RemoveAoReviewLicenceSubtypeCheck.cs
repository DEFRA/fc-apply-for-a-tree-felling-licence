using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAoReviewLicenceSubtypeCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LicenceSubTypeChecked",
                schema: "FellingLicenceApplications",
                table: "AdminOfficerReview");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "LicenceSubTypeChecked",
                schema: "FellingLicenceApplications",
                table: "AdminOfficerReview",
                type: "boolean",
                nullable: true);
        }
    }
}
