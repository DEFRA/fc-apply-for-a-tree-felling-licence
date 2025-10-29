using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    [ExcludeFromCodeCoverage]
    /// <inheritdoc />
    public partial class AddEnvironmentalImpactAssessmentEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnvironmentalImpactAssessmentStatus",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplicationStepStatus",
                type: "boolean",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EnvironmentalImpactAssessment",
                schema: "FellingLicenceApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    FellingLicenceApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    HasApplicationBeenCompleted = table.Column<bool>(type: "boolean", nullable: true),
                    HasApplicationBeenSent = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnvironmentalImpactAssessment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnvironmentalImpactAssessment_FellingLicenceApplication_Fel~",
                        column: x => x.FellingLicenceApplicationId,
                        principalSchema: "FellingLicenceApplications",
                        principalTable: "FellingLicenceApplication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnvironmentalImpactAssessment_FellingLicenceApplicationId",
                schema: "FellingLicenceApplications",
                table: "EnvironmentalImpactAssessment",
                column: "FellingLicenceApplicationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnvironmentalImpactAssessment",
                schema: "FellingLicenceApplications");

            migrationBuilder.DropColumn(
                name: "EnvironmentalImpactAssessmentStatus",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplicationStepStatus");
        }
    }
}
