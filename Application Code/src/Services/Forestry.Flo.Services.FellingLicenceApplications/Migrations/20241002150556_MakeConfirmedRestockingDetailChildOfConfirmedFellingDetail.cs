using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class MakeConfirmedRestockingDetailChildOfConfirmedFellingDetail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConfirmedFellingDetailId",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql(@"
UPDATE ""FellingLicenceApplications"".""ConfirmedRestockingDetail"" crd
SET ""ConfirmedFellingDetailId"" = cfd.""Id""
FROM ""FellingLicenceApplications"".""SubmittedFlaPropertyCompartment"" sfpc
JOIN ""FellingLicenceApplications"".""ConfirmedFellingDetail"" cfd 
    ON cfd.""SubmittedFlaPropertyCompartmentId"" = sfpc.""Id"" 
WHERE sfpc.""Id"" = crd.""SubmittedFlaPropertyCompartmentId"";
");
            migrationBuilder.CreateIndex(
                name: "IX_ConfirmedRestockingDetail_ConfirmedFellingDetailId",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail",
                column: "ConfirmedFellingDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfirmedRestockingDetail_SubmittedFlaPropertyCompartmentId~",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail",
                columns: new[] { "SubmittedFlaPropertyCompartmentId", "ConfirmedFellingDetailId", "RestockingProposal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfirmedFellingDetail_SubmittedFlaPropertyCompartmentId_Op~",
                schema: "FellingLicenceApplications",
                table: "ConfirmedFellingDetail",
                columns: new[] { "SubmittedFlaPropertyCompartmentId", "OperationType" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ConfirmedRestockingDetail_ConfirmedFellingDetail_ConfirmedF~",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail",
                column: "ConfirmedFellingDetailId",
                principalSchema: "FellingLicenceApplications",
                principalTable: "ConfirmedFellingDetail",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConfirmedRestockingDetail_ConfirmedFellingDetail_ConfirmedF~",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail");

            migrationBuilder.DropIndex(
                name: "IX_ConfirmedRestockingDetail_ConfirmedFellingDetailId",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail");

            migrationBuilder.DropIndex(
                name: "IX_ConfirmedRestockingDetail_SubmittedFlaPropertyCompartmentId~",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail");

            migrationBuilder.DropIndex(
                name: "IX_ConfirmedFellingDetail_SubmittedFlaPropertyCompartmentId_Op~",
                schema: "FellingLicenceApplications",
                table: "ConfirmedFellingDetail");

            migrationBuilder.DropColumn(
                name: "ConfirmedFellingDetailId",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail");
        }
    }
}
