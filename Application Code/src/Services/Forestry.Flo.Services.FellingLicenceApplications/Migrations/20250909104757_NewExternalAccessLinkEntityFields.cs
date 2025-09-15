using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    public partial class NewExternalAccessLinkEntityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LinkType",
                schema: "FellingLicenceApplications",
                table: "ExternalAccessLink",
                type: "text",
                nullable: false,
                defaultValue: $"{ExternalAccessLinkType.ConsulteeInvite.ToString()}");

            migrationBuilder.AddColumn<string>(
                name: "SharedSupportingDocuments",
                schema: "FellingLicenceApplications",
                table: "ExternalAccessLink",
                type: "text",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LinkType",
                schema: "FellingLicenceApplications",
                table: "ExternalAccessLink");

            migrationBuilder.DropColumn(
                name: "SharedSupportingDocuments",
                schema: "FellingLicenceApplications",
                table: "ExternalAccessLink");
        }
    }
}
