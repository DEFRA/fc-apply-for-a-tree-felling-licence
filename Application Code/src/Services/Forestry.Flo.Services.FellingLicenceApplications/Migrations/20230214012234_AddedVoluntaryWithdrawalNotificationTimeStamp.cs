using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    public partial class AddedVoluntaryWithdrawalNotificationTimeStamp : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "VoluntaryWithdrawalNotificationTimeStamp",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplication",
                type: "timestamp with time zone",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VoluntaryWithdrawalNotificationTimeStamp",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplication");
        }
    }
}
