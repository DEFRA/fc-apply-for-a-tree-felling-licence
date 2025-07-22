using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    public partial class AddWoodlandOfficerReview : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AttachedByType",
                schema: "FellingLicenceApplications",
                table: "Document",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateTable(
                name: "WoodlandOfficerReview",
                schema: "FellingLicenceApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    FellingLicenceApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    LandInformationSearchChecked = table.Column<bool>(type: "boolean", nullable: true),
                    AreProposalsUkfsCompliant = table.Column<bool>(type: "boolean", nullable: true),
                    TpoOrCaDeclared = table.Column<bool>(type: "boolean", nullable: true),
                    IsApplicationValid = table.Column<bool>(type: "boolean", nullable: true),
                    EiaThresholdExceeded = table.Column<bool>(type: "boolean", nullable: true),
                    EiaTrackerCompleted = table.Column<bool>(type: "boolean", nullable: true),
                    EiaChecklistDone = table.Column<bool>(type: "boolean", nullable: true),
                    Pw14ChecksComplete = table.Column<bool>(type: "boolean", nullable: false),
                    IsConditional = table.Column<bool>(type: "boolean", nullable: true),
                    ConditionsToApplicantDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WoodlandOfficerReviewComplete = table.Column<bool>(type: "boolean", nullable: false),
                    RecommendedLicenceDuration = table.Column<string>(type: "text", nullable: true),
                    SiteVisitNotNeeded = table.Column<bool>(type: "boolean", nullable: false),
                    SiteVisitArtefactsCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SiteVisitNotesRetrieved = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WoodlandOfficerReview", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WoodlandOfficerReview_FellingLicenceApplication_FellingLice~",
                        column: x => x.FellingLicenceApplicationId,
                        principalSchema: "FellingLicenceApplications",
                        principalTable: "FellingLicenceApplication",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_WoodlandOfficerReview_FellingLicenceApplicationId",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview",
                column: "FellingLicenceApplicationId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WoodlandOfficerReview",
                schema: "FellingLicenceApplications");

            migrationBuilder.AlterColumn<int>(
                name: "AttachedByType",
                schema: "FellingLicenceApplications",
                table: "Document",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
