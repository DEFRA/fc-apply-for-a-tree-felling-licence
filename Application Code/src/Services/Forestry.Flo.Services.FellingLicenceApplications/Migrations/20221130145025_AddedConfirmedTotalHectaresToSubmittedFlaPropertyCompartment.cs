using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    public partial class AddedConfirmedTotalHectaresToSubmittedFlaPropertyCompartment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ConfirmedTotalHectares",
                schema: "FellingLicenceApplications",
                table: "SubmittedFlaPropertyCompartment",
                type: "double precision",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfirmedTotalHectares",
                schema: "FellingLicenceApplications",
                table: "SubmittedFlaPropertyCompartment");

        }
    }
}
