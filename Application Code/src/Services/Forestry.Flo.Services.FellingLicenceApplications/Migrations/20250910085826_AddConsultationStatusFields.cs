using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    public partial class AddConsultationStatusFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ApplicationNeedsConsultations",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview",
                type: "boolean",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"FellingLicenceApplications\".\"WoodlandOfficerReview\" SET \"ApplicationNeedsConsultations\" = true WHERE \"FellingLicenceApplicationId\" IN (SELECT \"FellingLicenceApplicationId\" FROM \"FellingLicenceApplications\".\"ExternalAccessLink\");");

            migrationBuilder.AddColumn<bool>(
                name: "ConsultationsComplete",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicationNeedsConsultations",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview");

            migrationBuilder.DropColumn(
                name: "ConsultationsComplete",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview");
        }
    }
}
