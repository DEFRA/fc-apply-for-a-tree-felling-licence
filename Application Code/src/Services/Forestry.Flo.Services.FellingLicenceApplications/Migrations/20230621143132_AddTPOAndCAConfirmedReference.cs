using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class AddTPOAndCAConfirmedReference : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConservationAreaReference",
                schema: "FellingLicenceApplications",
                table: "ConfirmedFellingDetail",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TreePreservationOrderReference",
                schema: "FellingLicenceApplications",
                table: "ConfirmedFellingDetail",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConservationAreaReference",
                schema: "FellingLicenceApplications",
                table: "ConfirmedFellingDetail");

            migrationBuilder.DropColumn(
                name: "TreePreservationOrderReference",
                schema: "FellingLicenceApplications",
                table: "ConfirmedFellingDetail");
        }
    }
}
