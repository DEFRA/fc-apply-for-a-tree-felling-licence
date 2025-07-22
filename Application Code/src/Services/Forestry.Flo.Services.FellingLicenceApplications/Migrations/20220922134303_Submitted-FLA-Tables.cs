using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    public partial class SubmittedFLATables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubmittedFlaPropertyDetail",
                schema: "FellingLicenceApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    FellingLicenceApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    OSGridReference = table.Column<string>(type: "text", nullable: true),
                    NearestTown = table.Column<string>(type: "text", nullable: true),
                    HasWoodlandManagementPlan = table.Column<bool>(type: "boolean", nullable: false),
                    WoodlandManagementPlanReference = table.Column<string>(type: "text", nullable: true),
                    IsWoodlandCertificationScheme = table.Column<bool>(type: "boolean", nullable: true),
                    WoodlandCertificationSchemeReference = table.Column<string>(type: "text", nullable: true),
                    WoodlandOwnerId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmittedFlaPropertyDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmittedFlaPropertyDetail_FellingLicenceApplication_Fellin~",
                        column: x => x.FellingLicenceApplicationId,
                        principalSchema: "FellingLicenceApplications",
                        principalTable: "FellingLicenceApplication",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SubmittedFlaPropertyCompartment",
                schema: "FellingLicenceApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    SubmittedFlaPropertyDetailId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompartmentNumber = table.Column<string>(type: "text", nullable: false),
                    SubCompartmentName = table.Column<string>(type: "text", nullable: true),
                    TotalHectares = table.Column<double>(type: "double precision", nullable: true),
                    WoodlandName = table.Column<string>(type: "text", nullable: true),
                    Designation = table.Column<string>(type: "text", nullable: true),
                    GISData = table.Column<string>(type: "text", nullable: true),
                    PropertyProfileId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmittedFlaPropertyCompartment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmittedFlaPropertyCompartment_SubmittedFlaPropertyDetail_~",
                        column: x => x.SubmittedFlaPropertyDetailId,
                        principalSchema: "FellingLicenceApplications",
                        principalTable: "SubmittedFlaPropertyDetail",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedFlaPropertyCompartment_SubmittedFlaPropertyDetailId",
                schema: "FellingLicenceApplications",
                table: "SubmittedFlaPropertyCompartment",
                column: "SubmittedFlaPropertyDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedFlaPropertyDetail_FellingLicenceApplicationId",
                schema: "FellingLicenceApplications",
                table: "SubmittedFlaPropertyDetail",
                column: "FellingLicenceApplicationId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubmittedFlaPropertyCompartment",
                schema: "FellingLicenceApplications");

            migrationBuilder.DropTable(
                name: "SubmittedFlaPropertyDetail",
                schema: "FellingLicenceApplications");
        }
    }
}
