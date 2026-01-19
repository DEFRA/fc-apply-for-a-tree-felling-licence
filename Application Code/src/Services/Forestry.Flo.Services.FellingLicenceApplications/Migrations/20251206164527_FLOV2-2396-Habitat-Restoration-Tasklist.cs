using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    public partial class FLOV22396HabitatRestorationTasklist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HabitatRestorationStatus",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplicationStepStatus",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPriorityOpenHabitat",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplication",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HabitatRestorationStatus",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplicationStepStatus");

            migrationBuilder.DropColumn(
                name: "IsPriorityOpenHabitat",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplication");
        }
    }
}
