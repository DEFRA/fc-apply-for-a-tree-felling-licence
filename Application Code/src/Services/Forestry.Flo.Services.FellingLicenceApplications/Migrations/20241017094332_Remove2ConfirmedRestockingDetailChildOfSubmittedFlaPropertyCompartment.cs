using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    public partial class Remove2ConfirmedRestockingDetailChildOfSubmittedFlaPropertyCompartment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConfirmedRestockingDetail_SubmittedFlaPropertyCompartment_S~",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail");

            migrationBuilder.DropIndex(
                name: "IX_ConfirmedFellingDetail_SubmittedFlaPropertyCompartmentId",
                schema: "FellingLicenceApplications",
                table: "ConfirmedFellingDetail");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ConfirmedFellingDetail_SubmittedFlaPropertyCompartmentId",
                schema: "FellingLicenceApplications",
                table: "ConfirmedFellingDetail",
                column: "SubmittedFlaPropertyCompartmentId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ConfirmedRestockingDetail_SubmittedFlaPropertyCompartment_S~",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail",
                column: "SubmittedFlaPropertyCompartmentId",
                principalSchema: "FellingLicenceApplications",
                principalTable: "SubmittedFlaPropertyCompartment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
