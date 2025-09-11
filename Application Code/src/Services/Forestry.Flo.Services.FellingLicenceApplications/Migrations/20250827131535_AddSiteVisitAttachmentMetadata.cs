using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteVisitAttachmentMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SiteVisitEvidence",
                schema: "FellingLicenceApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    WoodlandOfficerReviewId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: true),
                    Comment = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteVisitEvidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteVisitEvidence_WoodlandOfficerReview_WoodlandOfficerRevi~",
                        column: x => x.WoodlandOfficerReviewId,
                        principalSchema: "FellingLicenceApplications",
                        principalTable: "WoodlandOfficerReview",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SiteVisitEvidence_WoodlandOfficerReviewId",
                schema: "FellingLicenceApplications",
                table: "SiteVisitEvidence",
                column: "WoodlandOfficerReviewId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SiteVisitEvidence",
                schema: "FellingLicenceApplications");
        }
    }
}
