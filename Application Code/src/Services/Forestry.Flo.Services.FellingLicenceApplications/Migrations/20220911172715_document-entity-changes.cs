using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    public partial class documententitychanges : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "FellingLicenceApplications",
                table: "Document");

            migrationBuilder.DropColumn(
                name: "DeletionTimestamp",
                schema: "FellingLicenceApplications",
                table: "Document");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "FellingLicenceApplications",
                table: "Document");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                schema: "FellingLicenceApplications",
                table: "Document",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                schema: "FellingLicenceApplications",
                table: "Document");

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                schema: "FellingLicenceApplications",
                table: "Document",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletionTimestamp",
                schema: "FellingLicenceApplications",
                table: "Document",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "FellingLicenceApplications",
                table: "Document",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
