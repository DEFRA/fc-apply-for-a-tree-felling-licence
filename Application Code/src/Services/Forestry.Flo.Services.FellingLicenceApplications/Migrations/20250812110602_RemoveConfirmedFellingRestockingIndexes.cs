using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    [ExcludeFromCodeCoverage]
    /// <inheritdoc />
    public partial class RemoveConfirmedFellingRestockingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ConfirmedRestockingDetail_SubmittedFlaPropertyCompartmentId~",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail");

            migrationBuilder.DropIndex(
                name: "IX_ConfirmedFellingDetail_SubmittedFlaPropertyCompartmentId_Op~",
                schema: "FellingLicenceApplications",
                table: "ConfirmedFellingDetail");

            migrationBuilder.CreateIndex(
                name: "IX_ConfirmedFellingDetail_SubmittedFlaPropertyCompartmentId",
                schema: "FellingLicenceApplications",
                table: "ConfirmedFellingDetail",
                column: "SubmittedFlaPropertyCompartmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ConfirmedFellingDetail_SubmittedFlaPropertyCompartmentId",
                schema: "FellingLicenceApplications",
                table: "ConfirmedFellingDetail");

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
        }
    }
}
