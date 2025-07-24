using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    public partial class AddNameOfWood : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NameOfWood",
                schema: "FellingLicenceApplications",
                table: "SubmittedFlaPropertyDetail",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NameOfWood",
                schema: "FellingLicenceApplications",
                table: "SubmittedFlaPropertyDetail");
        }
    }
}
