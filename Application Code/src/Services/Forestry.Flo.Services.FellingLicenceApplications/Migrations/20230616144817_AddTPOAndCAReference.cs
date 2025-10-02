using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class AddTPOAndCAReference : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConservationAreaReference",
                schema: "FellingLicenceApplications",
                table: "ProposedFellingDetail",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TreePreservationOrderReference",
                schema: "FellingLicenceApplications",
                table: "ProposedFellingDetail",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConservationAreaReference",
                schema: "FellingLicenceApplications",
                table: "ProposedFellingDetail");

            migrationBuilder.DropColumn(
                name: "TreePreservationOrderReference",
                schema: "FellingLicenceApplications",
                table: "ProposedFellingDetail");
        }
    }
}
