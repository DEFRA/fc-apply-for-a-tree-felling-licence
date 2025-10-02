using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class UpdateCascades : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConfirmedFellingDetail_SubmittedFlaPropertyCompartment_Subm~",
                schema: "FellingLicenceApplications",
                table: "ConfirmedFellingDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_ConfirmedRestockingDetail_SubmittedFlaPropertyCompartment_S~",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_PublicRegister_FellingLicenceApplication_FellingLicenceAppl~",
                schema: "FellingLicenceApplications",
                table: "PublicRegister");

            migrationBuilder.DropForeignKey(
                name: "FK_SubmittedFlaPropertyDetail_FellingLicenceApplication_Fellin~",
                schema: "FellingLicenceApplications",
                table: "SubmittedFlaPropertyDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_WoodlandOfficerReview_FellingLicenceApplication_FellingLice~",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview");

            migrationBuilder.AddForeignKey(
                name: "FK_ConfirmedFellingDetail_SubmittedFlaPropertyCompartment_Subm~",
                schema: "FellingLicenceApplications",
                table: "ConfirmedFellingDetail",
                column: "SubmittedFlaPropertyCompartmentId",
                principalSchema: "FellingLicenceApplications",
                principalTable: "SubmittedFlaPropertyCompartment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ConfirmedRestockingDetail_SubmittedFlaPropertyCompartment_S~",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail",
                column: "SubmittedFlaPropertyCompartmentId",
                principalSchema: "FellingLicenceApplications",
                principalTable: "SubmittedFlaPropertyCompartment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PublicRegister_FellingLicenceApplication_FellingLicenceAppl~",
                schema: "FellingLicenceApplications",
                table: "PublicRegister",
                column: "FellingLicenceApplicationId",
                principalSchema: "FellingLicenceApplications",
                principalTable: "FellingLicenceApplication",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SubmittedFlaPropertyDetail_FellingLicenceApplication_Fellin~",
                schema: "FellingLicenceApplications",
                table: "SubmittedFlaPropertyDetail",
                column: "FellingLicenceApplicationId",
                principalSchema: "FellingLicenceApplications",
                principalTable: "FellingLicenceApplication",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WoodlandOfficerReview_FellingLicenceApplication_FellingLice~",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview",
                column: "FellingLicenceApplicationId",
                principalSchema: "FellingLicenceApplications",
                principalTable: "FellingLicenceApplication",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConfirmedFellingDetail_SubmittedFlaPropertyCompartment_Subm~",
                schema: "FellingLicenceApplications",
                table: "ConfirmedFellingDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_ConfirmedRestockingDetail_SubmittedFlaPropertyCompartment_S~",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_PublicRegister_FellingLicenceApplication_FellingLicenceAppl~",
                schema: "FellingLicenceApplications",
                table: "PublicRegister");

            migrationBuilder.DropForeignKey(
                name: "FK_SubmittedFlaPropertyDetail_FellingLicenceApplication_Fellin~",
                schema: "FellingLicenceApplications",
                table: "SubmittedFlaPropertyDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_WoodlandOfficerReview_FellingLicenceApplication_FellingLice~",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview");

            migrationBuilder.AddForeignKey(
                name: "FK_ConfirmedFellingDetail_SubmittedFlaPropertyCompartment_Subm~",
                schema: "FellingLicenceApplications",
                table: "ConfirmedFellingDetail",
                column: "SubmittedFlaPropertyCompartmentId",
                principalSchema: "FellingLicenceApplications",
                principalTable: "SubmittedFlaPropertyCompartment",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ConfirmedRestockingDetail_SubmittedFlaPropertyCompartment_S~",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail",
                column: "SubmittedFlaPropertyCompartmentId",
                principalSchema: "FellingLicenceApplications",
                principalTable: "SubmittedFlaPropertyCompartment",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PublicRegister_FellingLicenceApplication_FellingLicenceAppl~",
                schema: "FellingLicenceApplications",
                table: "PublicRegister",
                column: "FellingLicenceApplicationId",
                principalSchema: "FellingLicenceApplications",
                principalTable: "FellingLicenceApplication",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SubmittedFlaPropertyDetail_FellingLicenceApplication_Fellin~",
                schema: "FellingLicenceApplications",
                table: "SubmittedFlaPropertyDetail",
                column: "FellingLicenceApplicationId",
                principalSchema: "FellingLicenceApplications",
                principalTable: "FellingLicenceApplication",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WoodlandOfficerReview_FellingLicenceApplication_FellingLice~",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview",
                column: "FellingLicenceApplicationId",
                principalSchema: "FellingLicenceApplications",
                principalTable: "FellingLicenceApplication",
                principalColumn: "Id");
        }
    }
}
