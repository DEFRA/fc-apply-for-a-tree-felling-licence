using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    public partial class AddTermsAndConditionsAcceptedFellingLicenceApplication : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TermsAndConditionsAccepted",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplication",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TermsAndConditionsAccepted",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplication");
        }
    }
}
