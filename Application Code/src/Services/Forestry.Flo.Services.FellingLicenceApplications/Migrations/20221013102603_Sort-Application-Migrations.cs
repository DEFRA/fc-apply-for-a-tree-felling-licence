using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class SortApplicationMigrations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Species",
                schema: "FellingLicenceApplications",
                table: "RestockingOutcome",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Species",
                schema: "FellingLicenceApplications",
                table: "FellingOutcome",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateTable(
                name: "CaseNote",
                schema: "FellingLicenceApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FellingLicenceApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    VisibleToApplicant = table.Column<bool>(type: "boolean", nullable: false),
                    VisibleToConsultee = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUser = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseNote", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseNote_FellingLicenceApplication_FellingLicenceApplicatio~",
                        column: x => x.FellingLicenceApplicationId,
                        principalSchema: "FellingLicenceApplications",
                        principalTable: "FellingLicenceApplication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaseNote_FellingLicenceApplicationId",
                schema: "FellingLicenceApplications",
                table: "CaseNote",
                column: "FellingLicenceApplicationId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaseNote",
                schema: "FellingLicenceApplications");

            migrationBuilder.AlterColumn<int>(
                name: "Species",
                schema: "FellingLicenceApplications",
                table: "RestockingOutcome",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "Species",
                schema: "FellingLicenceApplications",
                table: "FellingOutcome",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
