using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    public partial class PawsRestoration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRestoringCompartment",
                schema: "FellingLicenceApplications",
                table: "ProposedCompartmentDesignations",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RestorationDetails",
                schema: "FellingLicenceApplications",
                table: "ProposedCompartmentDesignations",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRestoringCompartment",
                schema: "FellingLicenceApplications",
                table: "ProposedCompartmentDesignations");

            migrationBuilder.DropColumn(
                name: "RestorationDetails",
                schema: "FellingLicenceApplications",
                table: "ProposedCompartmentDesignations");
        }
    }
}
