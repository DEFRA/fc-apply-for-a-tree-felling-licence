using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    public partial class FLOV22369ApprovedInErrordatastorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApprovedInError",
                schema: "FellingLicenceApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    FellingLicenceApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    OldReferenceNumber = table.Column<string>(type: "text", nullable: false),
                    ReasonExpiryDate = table.Column<bool>(type: "boolean", nullable: false),
                    ReasonSupplementaryPoints = table.Column<bool>(type: "boolean", nullable: false),
                    ReasonOther = table.Column<bool>(type: "boolean", nullable: false),
                    CaseNote = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovedInError", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovedInError_FellingLicenceApplication_FellingLicenceApp~",
                        column: x => x.FellingLicenceApplicationId,
                        principalSchema: "FellingLicenceApplications",
                        principalTable: "FellingLicenceApplication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedInError_FellingLicenceApplicationId",
                schema: "FellingLicenceApplications",
                table: "ApprovedInError",
                column: "FellingLicenceApplicationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovedInError",
                schema: "FellingLicenceApplications");
        }
    }
}
