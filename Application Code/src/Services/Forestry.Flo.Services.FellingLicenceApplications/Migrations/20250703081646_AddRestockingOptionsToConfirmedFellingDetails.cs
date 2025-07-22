using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    public partial class AddRestockingOptionsToConfirmedFellingDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRestocking",
                schema: "FellingLicenceApplications",
                table: "ConfirmedFellingDetail",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoRestockingReason",
                schema: "FellingLicenceApplications",
                table: "ConfirmedFellingDetail",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRestocking",
                schema: "FellingLicenceApplications",
                table: "ConfirmedFellingDetail");

            migrationBuilder.DropColumn(
                name: "NoRestockingReason",
                schema: "FellingLicenceApplications",
                table: "ConfirmedFellingDetail");
        }
    }
}
