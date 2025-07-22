using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    public partial class DocumentVisibility : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "VisibleToApplicant",
                schema: "FellingLicenceApplications",
                table: "Document",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "VisibleToConsultee",
                schema: "FellingLicenceApplications",
                table: "Document",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VisibleToApplicant",
                schema: "FellingLicenceApplications",
                table: "Document");

            migrationBuilder.DropColumn(
                name: "VisibleToConsultee",
                schema: "FellingLicenceApplications",
                table: "Document");
        }
    }
}
