using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    public partial class OperationDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Measures",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplication",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProposedTiming",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplication",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Measures",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplication");

            migrationBuilder.DropColumn(
                name: "ProposedTiming",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplication");
        }
    }
}
