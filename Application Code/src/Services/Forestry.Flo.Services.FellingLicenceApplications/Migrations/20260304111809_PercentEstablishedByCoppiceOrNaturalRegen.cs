using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    public partial class PercentEstablishedByCoppiceOrNaturalRegen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "PercentageEstablishedByCoppiceOrNaturalRegen",
                schema: "FellingLicenceApplications",
                table: "ProposedRestockingDetail",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PercentageEstablishedByCoppiceOrNaturalRegen",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PercentageEstablishedByCoppiceOrNaturalRegen",
                schema: "FellingLicenceApplications",
                table: "ProposedRestockingDetail");

            migrationBuilder.DropColumn(
                name: "PercentageEstablishedByCoppiceOrNaturalRegen",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail");
        }
    }
}
