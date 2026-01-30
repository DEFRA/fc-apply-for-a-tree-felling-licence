using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldForOldConditionsToApplicantDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OldConditionsSentToApplicantDate",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OldConditionsSentToApplicantDate",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview");
        }
    }
}
