using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    public partial class MakeProposedRestockingDetailChildOfProposedFellingDetail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProposedFellingDetailsId",
                schema: "FellingLicenceApplications",
                table: "ProposedRestockingDetail",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("update \"FellingLicenceApplications\".\"ProposedRestockingDetail\" prd\r\nset \"ProposedFellingDetailsId\" = pfd.\"Id\"\r\nfrom \"FellingLicenceApplications\".\"LinkedPropertyProfile\" lpp\r\njoin \"FellingLicenceApplications\".\"ProposedFellingDetail\" pfd on pfd.\"LinkedPropertyProfileId\" = lpp.\"Id\"\r\nwhere lpp.\"Id\" = prd.\"LinkedPropertyProfileId\";");

            migrationBuilder.DropForeignKey(
                name: "FK_ProposedRestockingDetail_LinkedPropertyProfile_LinkedProper~",
                schema: "FellingLicenceApplications",
                table: "ProposedRestockingDetail");

            migrationBuilder.DropIndex(
                name: "IX_ProposedRestockingDetail_LinkedPropertyProfileId",
                schema: "FellingLicenceApplications",
                table: "ProposedRestockingDetail");

            migrationBuilder.DropColumn(
                name: "LinkedPropertyProfileId",
                schema: "FellingLicenceApplications",
                table: "ProposedRestockingDetail");

            migrationBuilder.DropIndex(
                name: "IX_ProposedFellingDetail_LinkedPropertyProfileId",
                schema: "FellingLicenceApplications",
                table: "ProposedFellingDetail");

            migrationBuilder.CreateIndex(
                name: "IX_ProposedRestockingDetail_ProposedFellingDetailsId_PropertyP~",
                schema: "FellingLicenceApplications",
                table: "ProposedRestockingDetail",
                columns: new[] { "ProposedFellingDetailsId", "PropertyProfileCompartmentId", "RestockingProposal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProposedFellingDetail_LinkedPropertyProfileId_PropertyProfi~",
                schema: "FellingLicenceApplications",
                table: "ProposedFellingDetail",
                columns: new[] { "LinkedPropertyProfileId", "PropertyProfileCompartmentId", "OperationType" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProposedRestockingDetail_ProposedFellingDetail_ProposedFell~",
                schema: "FellingLicenceApplications",
                table: "ProposedRestockingDetail",
                column: "ProposedFellingDetailsId",
                principalSchema: "FellingLicenceApplications",
                principalTable: "ProposedFellingDetail",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LinkedPropertyProfileId",
                schema: "FellingLicenceApplications",
                table: "ProposedRestockingDetail",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("update \"FellingLicenceApplications\".\"ProposedRestockingDetail\" prd\r\nset \"LinkedPropertyProfileId\" = lpp.\"Id\"\r\nfrom \"FellingLicenceApplications\".\"ProposedFellingDetail\" pfd\r\njoin \"FellingLicenceApplications\".\"LinkedPropertyProfile\" lpp on pfd.\"LinkedPropertyProfileId\" = lpp.\"Id\"\r\nwhere pfd.\"Id\" = prd.\"ProposedFellingDetailsId\";");

            migrationBuilder.DropForeignKey(
                name: "FK_ProposedRestockingDetail_ProposedFellingDetail_ProposedFell~",
                schema: "FellingLicenceApplications",
                table: "ProposedRestockingDetail");

            migrationBuilder.DropIndex(
                name: "IX_ProposedRestockingDetail_ProposedFellingDetailsId_PropertyP~",
                schema: "FellingLicenceApplications",
                table: "ProposedRestockingDetail");

            migrationBuilder.DropColumn(
                name: "ProposedFellingDetailsId",
                schema: "FellingLicenceApplications",
                table: "ProposedRestockingDetail");

            migrationBuilder.DropIndex(
                name: "IX_ProposedFellingDetail_LinkedPropertyProfileId_PropertyProfi~",
                schema: "FellingLicenceApplications",
                table: "ProposedFellingDetail");

            migrationBuilder.CreateIndex(
                name: "IX_ProposedRestockingDetail_LinkedPropertyProfileId",
                schema: "FellingLicenceApplications",
                table: "ProposedRestockingDetail",
                column: "LinkedPropertyProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ProposedFellingDetail_LinkedPropertyProfileId",
                schema: "FellingLicenceApplications",
                table: "ProposedFellingDetail",
                column: "LinkedPropertyProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProposedRestockingDetail_LinkedPropertyProfile_LinkedProper~",
                schema: "FellingLicenceApplications",
                table: "ProposedRestockingDetail",
                column: "LinkedPropertyProfileId",
                principalSchema: "FellingLicenceApplications",
                principalTable: "LinkedPropertyProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
