using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    public partial class ApplicantUpdatesToReviewAmendmentsEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FellingAndRestockingAmendmentReview",
                schema: "FellingLicenceApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    WoodlandOfficerReviewId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmendmentsSentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResponseDeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResponseReceivedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApplicantAgreed = table.Column<bool>(type: "boolean", nullable: true),
                    ApplicantDisagreementReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FellingAndRestockingAmendmentReview", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FellingAndRestockingAmendmentReview_WoodlandOfficerReview_W~",
                        column: x => x.WoodlandOfficerReviewId,
                        principalSchema: "FellingLicenceApplications",
                        principalTable: "WoodlandOfficerReview",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FellingAndRestockingAmendmentReview_WoodlandOfficerReviewId~",
                schema: "FellingLicenceApplications",
                table: "FellingAndRestockingAmendmentReview",
                columns: new[] { "WoodlandOfficerReviewId", "ResponseReceivedDate" },
                unique: true,
                filter: "\"ResponseReceivedDate\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FellingAndRestockingAmendmentReview",
                schema: "FellingLicenceApplications");
        }
    }
}
