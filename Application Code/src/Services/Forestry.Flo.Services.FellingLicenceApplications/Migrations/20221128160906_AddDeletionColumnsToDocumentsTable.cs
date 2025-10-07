using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class AddDeletionColumnsToDocumentsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "FellingLicenceApplications",
                table: "Document");

            migrationBuilder.DropColumn(
                name: "DeletionTimestamp",
                schema: "FellingLicenceApplications",
                table: "Document");
        }
    }
}
