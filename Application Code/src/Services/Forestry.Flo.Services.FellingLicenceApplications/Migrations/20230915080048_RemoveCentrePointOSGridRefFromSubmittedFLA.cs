using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    public partial class RemoveCentrePointOSGridRefFromSubmittedFLA : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CentrePoint",
                schema: "FellingLicenceApplications",
                table: "SubmittedFlaPropertyDetail");

            migrationBuilder.DropColumn(
                name: "OSGridReference",
                schema: "FellingLicenceApplications",
                table: "SubmittedFlaPropertyDetail");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CentrePoint",
                schema: "FellingLicenceApplications",
                table: "SubmittedFlaPropertyDetail",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OSGridReference",
                schema: "FellingLicenceApplications",
                table: "SubmittedFlaPropertyDetail",
                type: "text",
                nullable: true);
        }
    }
}
