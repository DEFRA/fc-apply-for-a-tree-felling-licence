using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    [ExcludeFromCodeCoverage]
    /// <inheritdoc />
    public partial class LarchCheckDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LarchCheckDetails",
                schema: "FellingLicenceApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    FellingLicenceApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfirmLarchOnly = table.Column<bool>(type: "boolean", nullable: true),
                    Zone1 = table.Column<bool>(type: "boolean", nullable: false),
                    Zone2 = table.Column<bool>(type: "boolean", nullable: false),
                    Zone3 = table.Column<bool>(type: "boolean", nullable: false),
                    ConfirmMoratorium = table.Column<bool>(type: "boolean", nullable: true),
                    ConfirmInspectionLog = table.Column<bool>(type: "boolean", nullable: false),
                    RecommendSplitApplicationDue = table.Column<int>(type: "integer", nullable: false),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LarchCheckDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LarchCheckDetails_FellingLicenceApplication_FellingLicenceA~",
                        column: x => x.FellingLicenceApplicationId,
                        principalSchema: "FellingLicenceApplications",
                        principalTable: "FellingLicenceApplication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LarchCheckDetails_FellingLicenceApplicationId",
                schema: "FellingLicenceApplications",
                table: "LarchCheckDetails",
                column: "FellingLicenceApplicationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LarchCheckDetails",
                schema: "FellingLicenceApplications");
        }
    }
}
