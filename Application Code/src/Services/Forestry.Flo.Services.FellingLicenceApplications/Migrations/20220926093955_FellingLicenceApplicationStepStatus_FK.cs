using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    public partial class FellingLicenceApplicationStepStatus_FK : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FellingLicenceApplicationId",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplicationStepStatus",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_FellingLicenceApplicationStepStatus_FellingLicenceApplicati~",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplicationStepStatus",
                column: "FellingLicenceApplicationId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FellingLicenceApplicationStepStatus_FellingLicenceApplicati~",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplicationStepStatus",
                column: "FellingLicenceApplicationId",
                principalSchema: "FellingLicenceApplications",
                principalTable: "FellingLicenceApplication",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FellingLicenceApplicationStepStatus_FellingLicenceApplicati~",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplicationStepStatus");

            migrationBuilder.DropIndex(
                name: "IX_FellingLicenceApplicationStepStatus_FellingLicenceApplicati~",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplicationStepStatus");

            migrationBuilder.DropColumn(
                name: "FellingLicenceApplicationId",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplicationStepStatus");
        }
    }
}
