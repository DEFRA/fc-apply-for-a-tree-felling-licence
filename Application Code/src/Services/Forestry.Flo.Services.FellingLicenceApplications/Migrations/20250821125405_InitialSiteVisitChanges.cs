using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    public partial class InitialSiteVisitChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SiteVisitNeeded",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview",
                type: "boolean",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE \"FellingLicenceApplications\".\"WoodlandOfficerReview\" SET \"SiteVisitNeeded\" = NOT \"SiteVisitNotNeeded\";");

            migrationBuilder.AddColumn<bool>(
                name: "SiteVisitArrangementsMade",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview",
                type: "boolean",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE \"FellingLicenceApplications\".\"WoodlandOfficerReview\" SET \"SiteVisitArrangementsMade\" = TRUE WHERE \"SiteVisitArtefactsCreated\" IS NOT NULL;");

            migrationBuilder.AddColumn<bool>(
                name: "SiteVisitComplete",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview",
                type: "boolean",
                defaultValue: false,
                nullable: false);

            migrationBuilder.Sql(
                "UPDATE \"FellingLicenceApplications\".\"WoodlandOfficerReview\" SET \"SiteVisitComplete\" = TRUE WHERE \"SiteVisitNotesRetrieved\" IS NOT NULL;");

            migrationBuilder.DropColumn(
                name: "SiteVisitArtefactsCreated",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview");

            migrationBuilder.DropColumn(
                name: "SiteVisitNotesRetrieved",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview");

            migrationBuilder.DropColumn(
                name: "SiteVisitNotNeeded",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SiteVisitNotNeeded",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                "UPDATE \"FellingLicenceApplications\".\"WoodlandOfficerReview\" SET \"SiteVisitNotNeeded\" = NOT \"SiteVisitNeeded\" WHERE \"SiteVisitNeeded\" IS NOT NULL;");

            migrationBuilder.DropColumn(
                name: "SiteVisitNeeded",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview");

            migrationBuilder.AddColumn<DateTime>(
                name: "SiteVisitArtefactsCreated",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE \"FellingLicenceApplications\".\"WoodlandOfficerReview\" SET \"SiteVisitArtefactsCreated\" = \"LastUpdatedDate\" WHERE \"SiteVisitArrangementsMade\" IS TRUE;");

            migrationBuilder.DropColumn(
                name: "SiteVisitArrangementsMade",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview");

            migrationBuilder.AddColumn<DateTime>(
                name: "SiteVisitNotesRetrieved",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE \"FellingLicenceApplications\".\"WoodlandOfficerReview\" SET \"SiteVisitNotesRetrieved\" = \"LastUpdatedDate\" WHERE \"SiteVisitComplete\" IS TRUE;");

            migrationBuilder.DropColumn(
                name: "SiteVisitComplete",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview");


        }
    }
}
