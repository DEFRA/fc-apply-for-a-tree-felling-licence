using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class SupportingDocumentUploadedBy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AttachedById",
                schema: "FellingLicenceApplications",
                table: "Document",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AttachedByType",
                schema: "FellingLicenceApplications",
                table: "Document",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachedById",
                schema: "FellingLicenceApplications",
                table: "Document");

            migrationBuilder.DropColumn(
                name: "AttachedByType",
                schema: "FellingLicenceApplications",
                table: "Document");
        }
    }
}
