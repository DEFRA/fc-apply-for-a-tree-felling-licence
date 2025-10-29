using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class AddedCreatedByUserIdCaseNotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove all data entries to avoid compatibility errors

            migrationBuilder.Sql("DELETE FROM \"FellingLicenceApplications\".\"CaseNote\";", true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                schema: "FellingLicenceApplications",
                table: "CaseNote",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                schema: "FellingLicenceApplications",
                table: "CaseNote");
        }
    }
}
