using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class RemoveConfirmedRestockingDetailChildOfSubmittedFlaPropertyCompartment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ConfirmedRestockingDetail_SubmittedFlaPropertyCompartmentId",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ConfirmedRestockingDetail_SubmittedFlaPropertyCompartmentId",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail",
                column: "SubmittedFlaPropertyCompartmentId",
                unique: true);
        }
    }
}
