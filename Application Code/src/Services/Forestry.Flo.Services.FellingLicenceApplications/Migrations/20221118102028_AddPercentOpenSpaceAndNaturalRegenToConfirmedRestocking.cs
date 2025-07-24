using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    public partial class AddPercentOpenSpaceAndNaturalRegenToConfirmedRestocking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PercentNaturalRegeneration",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PercentOpenSpace",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail",
                type: "integer",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PercentNaturalRegeneration",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail");

            migrationBuilder.DropColumn(
                name: "PercentOpenSpace",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail");
        }
    }
}
