using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
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
