using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    public partial class AdminOfficerReviewEia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AreAttachedFormsCorrect",
                schema: "FellingLicenceApplications",
                table: "EnvironmentalImpactAssessment",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EiaTrackerReferenceNumber",
                schema: "FellingLicenceApplications",
                table: "EnvironmentalImpactAssessment",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasTheEiaFormBeenReceived",
                schema: "FellingLicenceApplications",
                table: "EnvironmentalImpactAssessment",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EiaChecked",
                schema: "FellingLicenceApplications",
                table: "AdminOfficerReview",
                type: "boolean",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EnvironmentalImpactAssessmentRequestHistory",
                schema: "FellingLicenceApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    EnvironmentalImpactAssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestingUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RequestType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnvironmentalImpactAssessmentRequestHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnvironmentalImpactAssessmentRequestHistory_EnvironmentalIm~",
                        column: x => x.EnvironmentalImpactAssessmentId,
                        principalSchema: "FellingLicenceApplications",
                        principalTable: "EnvironmentalImpactAssessment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnvironmentalImpactAssessmentRequestHistory_EnvironmentalIm~",
                schema: "FellingLicenceApplications",
                table: "EnvironmentalImpactAssessmentRequestHistory",
                column: "EnvironmentalImpactAssessmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnvironmentalImpactAssessmentRequestHistory",
                schema: "FellingLicenceApplications");

            migrationBuilder.DropColumn(
                name: "AreAttachedFormsCorrect",
                schema: "FellingLicenceApplications",
                table: "EnvironmentalImpactAssessment");

            migrationBuilder.DropColumn(
                name: "EiaTrackerReferenceNumber",
                schema: "FellingLicenceApplications",
                table: "EnvironmentalImpactAssessment");

            migrationBuilder.DropColumn(
                name: "HasTheEiaFormBeenReceived",
                schema: "FellingLicenceApplications",
                table: "EnvironmentalImpactAssessment");

            migrationBuilder.DropColumn(
                name: "EiaChecked",
                schema: "FellingLicenceApplications",
                table: "AdminOfficerReview");
        }
    }
}
