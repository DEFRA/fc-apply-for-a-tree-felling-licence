using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    public partial class Submittedpropertydetailscascade : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubmittedFlaPropertyCompartment_SubmittedFlaPropertyDetail_~",
                schema: "FellingLicenceApplications",
                table: "SubmittedFlaPropertyCompartment");

            migrationBuilder.AddForeignKey(
                name: "FK_SubmittedFlaPropertyCompartment_SubmittedFlaPropertyDetail_~",
                schema: "FellingLicenceApplications",
                table: "SubmittedFlaPropertyCompartment",
                column: "SubmittedFlaPropertyDetailId",
                principalSchema: "FellingLicenceApplications",
                principalTable: "SubmittedFlaPropertyDetail",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubmittedFlaPropertyCompartment_SubmittedFlaPropertyDetail_~",
                schema: "FellingLicenceApplications",
                table: "SubmittedFlaPropertyCompartment");

            migrationBuilder.AddForeignKey(
                name: "FK_SubmittedFlaPropertyCompartment_SubmittedFlaPropertyDetail_~",
                schema: "FellingLicenceApplications",
                table: "SubmittedFlaPropertyCompartment",
                column: "SubmittedFlaPropertyDetailId",
                principalSchema: "FellingLicenceApplications",
                principalTable: "SubmittedFlaPropertyDetail",
                principalColumn: "Id");
        }
    }
}
