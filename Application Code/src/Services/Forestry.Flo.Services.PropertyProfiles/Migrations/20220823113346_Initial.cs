using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.PropertyProfiles.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "PropertyProfiles");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

            migrationBuilder.CreateTable(
                name: "PropertyProfile",
                schema: "PropertyProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
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
                    table.PrimaryKey("PK_PropertyProfile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropertyProfile_WoodlandOwner_WoodlandOwnerId",
                        column: x => x.WoodlandOwnerId,
                        principalSchema: "Applicants",
                        principalTable: "WoodlandOwner",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Compartment",
                schema: "PropertyProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
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
                    table.PrimaryKey("PK_Compartment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Compartment_PropertyProfile_PropertyProfileId",
                        column: x => x.PropertyProfileId,
                        principalSchema: "PropertyProfiles",
                        principalTable: "PropertyProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Compartment_CompartmentNumber_PropertyProfileId",
                schema: "PropertyProfiles",
                table: "Compartment",
                columns: new[] { "CompartmentNumber", "PropertyProfileId" },
                unique: true,
                filter: "\"SubCompartmentName\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Compartment_CompartmentNumber_SubCompartmentName_PropertyPr~",
                schema: "PropertyProfiles",
                table: "Compartment",
                columns: new[] { "CompartmentNumber", "SubCompartmentName", "PropertyProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Compartment_PropertyProfileId",
                schema: "PropertyProfiles",
                table: "Compartment",
                column: "PropertyProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyProfile_Name_WoodlandOwnerId",
                schema: "PropertyProfiles",
                table: "PropertyProfile",
                columns: new[] { "Name", "WoodlandOwnerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropertyProfile_WoodlandOwnerId",
                schema: "PropertyProfiles",
                table: "PropertyProfile",
                column: "WoodlandOwnerId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Compartment",
                schema: "PropertyProfiles");

            migrationBuilder.DropTable(
                name: "PropertyProfile",
                schema: "PropertyProfiles");
        }
    }
}
