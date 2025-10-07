using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class Add_Decision_PR_Fields_To_PR_Table : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RemovedTimestamp",
                schema: "FellingLicenceApplications",
                table: "PublicRegister",
                newName: "DecisionPublicRegisterRemovedTimestamp");

            migrationBuilder.RenameColumn(
                name: "PublicationTimestamp",
                schema: "FellingLicenceApplications",
                table: "PublicRegister",
                newName: "DecisionPublicRegisterPublicationTimestamp");

            migrationBuilder.RenameColumn(
                name: "ExpiryTimestamp",
                schema: "FellingLicenceApplications",
                table: "PublicRegister",
                newName: "DecisionPublicRegisterExpiryTimestamp");

            migrationBuilder.RenameColumn(
                name: "ExemptionReason",
                schema: "FellingLicenceApplications",
                table: "PublicRegister",
                newName: "WoodlandOfficerConsultationPublicRegisterExemptionReason");

            migrationBuilder.RenameColumn(
                name: "ExemptFromPublicRegister",
                schema: "FellingLicenceApplications",
                table: "PublicRegister",
                newName: "WoodlandOfficerSetAsExemptFromConsultationPublicRegister");

            migrationBuilder.AddColumn<DateTime>(
                name: "ConsultationPublicRegisterExpiryTimestamp",
                schema: "FellingLicenceApplications",
                table: "PublicRegister",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConsultationPublicRegisterPublicationTimestamp",
                schema: "FellingLicenceApplications",
                table: "PublicRegister",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConsultationPublicRegisterRemovedTimestamp",
                schema: "FellingLicenceApplications",
                table: "PublicRegister",
                type: "timestamp with time zone",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsultationPublicRegisterExpiryTimestamp",
                schema: "FellingLicenceApplications",
                table: "PublicRegister");

            migrationBuilder.DropColumn(
                name: "ConsultationPublicRegisterPublicationTimestamp",
                schema: "FellingLicenceApplications",
                table: "PublicRegister");

            migrationBuilder.DropColumn(
                name: "ConsultationPublicRegisterRemovedTimestamp",
                schema: "FellingLicenceApplications",
                table: "PublicRegister");

            migrationBuilder.RenameColumn(
                name: "WoodlandOfficerSetAsExemptFromConsultationPublicRegister",
                schema: "FellingLicenceApplications",
                table: "PublicRegister",
                newName: "ExemptFromPublicRegister");

            migrationBuilder.RenameColumn(
                name: "WoodlandOfficerConsultationPublicRegisterExemptionReason",
                schema: "FellingLicenceApplications",
                table: "PublicRegister",
                newName: "ExemptionReason");

            migrationBuilder.RenameColumn(
                name: "DecisionPublicRegisterRemovedTimestamp",
                schema: "FellingLicenceApplications",
                table: "PublicRegister",
                newName: "RemovedTimestamp");

            migrationBuilder.RenameColumn(
                name: "DecisionPublicRegisterPublicationTimestamp",
                schema: "FellingLicenceApplications",
                table: "PublicRegister",
                newName: "PublicationTimestamp");

            migrationBuilder.RenameColumn(
                name: "DecisionPublicRegisterExpiryTimestamp",
                schema: "FellingLicenceApplications",
                table: "PublicRegister",
                newName: "ExpiryTimestamp");
        }
    }
}
