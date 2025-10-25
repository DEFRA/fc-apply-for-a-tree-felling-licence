using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class FLOV22362FLOV22479SendremindernotificationtoApplicant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReminderNotificationTimeStamp",
                schema: "FellingLicenceApplications",
                table: "FellingAndRestockingAmendmentReview",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReminderNotificationTimeStamp",
                schema: "FellingLicenceApplications",
                table: "FellingAndRestockingAmendmentReview");
        }
    }
}
