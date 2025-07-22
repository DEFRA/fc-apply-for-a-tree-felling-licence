using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    public partial class AddRestockDecisionColumnsToProposedFellingDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRestocking",
                schema: "FellingLicenceApplications",
                table: "ProposedFellingDetail",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoRestockingReason",
                schema: "FellingLicenceApplications",
                table: "ProposedFellingDetail",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRestocking",
                schema: "FellingLicenceApplications",
                table: "ProposedFellingDetail");

            migrationBuilder.DropColumn(
                name: "NoRestockingReason",
                schema: "FellingLicenceApplications",
                table: "ProposedFellingDetail");
        }
    }
}
