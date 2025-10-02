using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class AdminOfficerReviewEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminOfficerReview",
                schema: "FellingLicenceApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    FellingLicenceApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentAuthorityFormChecked = table.Column<bool>(type: "boolean", nullable: true),
                    MappingChecked = table.Column<bool>(type: "boolean", nullable: true),
                    ConstraintsChecked = table.Column<bool>(type: "boolean", nullable: true),
                    LicenceSubTypeChecked = table.Column<bool>(type: "boolean", nullable: true),
                    AdminOfficerReviewComplete = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminOfficerReview", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminOfficerReview_FellingLicenceApplication_FellingLicence~",
                        column: x => x.FellingLicenceApplicationId,
                        principalSchema: "FellingLicenceApplications",
                        principalTable: "FellingLicenceApplication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminOfficerReview_FellingLicenceApplicationId",
                schema: "FellingLicenceApplications",
                table: "AdminOfficerReview",
                column: "FellingLicenceApplicationId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminOfficerReview",
                schema: "FellingLicenceApplications");
        }
    }
}
