using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    public partial class ExternalLinkAccessCodeGuid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            var script = "ALTER TABLE \"FellingLicenceApplications\".\"ExternalAccessLink\" ALTER COLUMN \"AccessCode\" TYPE uuid USING \"AccessCode\"::uuid;";
            migrationBuilder.Sql(script);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AccessCode",
                schema: "FellingLicenceApplications",
                table: "ExternalAccessLink",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }
    }
}
