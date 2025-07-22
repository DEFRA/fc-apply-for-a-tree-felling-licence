using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    public partial class AddCentrePointToSubmittedFlaPropertyDetail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CentrePoint",
                schema: "FellingLicenceApplications",
                table: "SubmittedFlaPropertyDetail",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CentrePoint",
                schema: "FellingLicenceApplications",
                table: "SubmittedFlaPropertyDetail");
        }
    }
}
