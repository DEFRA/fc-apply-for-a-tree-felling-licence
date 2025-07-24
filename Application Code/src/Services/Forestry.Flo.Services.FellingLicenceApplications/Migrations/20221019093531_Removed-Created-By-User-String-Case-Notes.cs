using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    public partial class RemovedCreatedByUserStringCaseNotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByUser",
                schema: "FellingLicenceApplications",
                table: "CaseNote");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByUser",
                schema: "FellingLicenceApplications",
                table: "CaseNote",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
