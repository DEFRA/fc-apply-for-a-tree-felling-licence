using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    public partial class ApproverReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApproverReview",
                schema: "FellingLicenceApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    FellingLicenceApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedStatus = table.Column<int>(type: "integer", nullable: false),
                    CheckedApplication = table.Column<bool>(type: "boolean", nullable: false),
                    CheckedDocumentation = table.Column<bool>(type: "boolean", nullable: false),
                    CheckedCaseNotes = table.Column<bool>(type: "boolean", nullable: false),
                    CheckedWOReview = table.Column<bool>(type: "boolean", nullable: false),
                    InformedApplicant = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovedLicenceDuration = table.Column<string>(type: "text", nullable: true),
                    DurationChangeReason = table.Column<string>(type: "text", nullable: true),
                    PublicRegisterPublish = table.Column<bool>(type: "boolean", nullable: true),
                    PublicRegisterExemptionReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApproverReview", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApproverReview_FellingLicenceApplication_FellingLicenceAppl~",
                        column: x => x.FellingLicenceApplicationId,
                        principalSchema: "FellingLicenceApplications",
                        principalTable: "FellingLicenceApplication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApproverReview_FellingLicenceApplicationId",
                schema: "FellingLicenceApplications",
                table: "ApproverReview",
                column: "FellingLicenceApplicationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApproverReview",
                schema: "FellingLicenceApplications");
        }
    }
}
