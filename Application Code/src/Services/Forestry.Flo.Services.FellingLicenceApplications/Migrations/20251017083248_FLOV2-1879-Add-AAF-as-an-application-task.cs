using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    public partial class FLOV21879AddAAFasanapplicationtask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AafStepStatus",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplicationStepStatus",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AafStepStatus",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplicationStepStatus");
        }
    }
}
