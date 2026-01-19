using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    public partial class FLOV22396HabitatRestorationExternal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HabitatRestoration",
                schema: "FellingLicenceApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkedPropertyProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyProfileCompartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    HabitatType = table.Column<int>(type: "integer", nullable: true),
                    WoodlandSpeciesType = table.Column<int>(type: "integer", nullable: true),
                    NativeBroadleaf = table.Column<bool>(type: "boolean", nullable: true),
                    ProductiveWoodland = table.Column<bool>(type: "boolean", nullable: true),
                    FelledEarly = table.Column<bool>(type: "boolean", nullable: true),
                    Completed = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HabitatRestoration", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HabitatRestoration_LinkedPropertyProfile_LinkedPropertyProf~",
                        column: x => x.LinkedPropertyProfileId,
                        principalSchema: "FellingLicenceApplications",
                        principalTable: "LinkedPropertyProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HabitatRestoration_LinkedPropertyProfileId",
                schema: "FellingLicenceApplications",
                table: "HabitatRestoration",
                column: "LinkedPropertyProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HabitatRestoration",
                schema: "FellingLicenceApplications");
        }
    }
}
