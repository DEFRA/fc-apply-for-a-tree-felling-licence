using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class AddConfirmedFellingAndRestocking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfirmedFellingDetail",
                schema: "FellingLicenceApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    SubmittedFlaPropertyCompartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationType = table.Column<int>(type: "integer", nullable: false),
                    AreaToBeFelled = table.Column<double>(type: "double precision", nullable: false),
                    NumberOfTrees = table.Column<int>(type: "integer", nullable: true),
                    TreeMarking = table.Column<string>(type: "text", nullable: true),
                    IsPartOfTreePreservationOrder = table.Column<bool>(type: "boolean", nullable: false),
                    IsWithinConservationArea = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfirmedFellingDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfirmedFellingDetail_SubmittedFlaPropertyCompartment_Subm~",
                        column: x => x.SubmittedFlaPropertyCompartmentId,
                        principalSchema: "FellingLicenceApplications",
                        principalTable: "SubmittedFlaPropertyCompartment",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ConfirmedRestockingDetail",
                schema: "FellingLicenceApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    SubmittedFlaPropertyCompartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RestockingProposal = table.Column<int>(type: "integer", nullable: false),
                    Area = table.Column<double>(type: "double precision", nullable: true),
                    PercentageOfRestockArea = table.Column<double>(type: "double precision", nullable: true),
                    RestockingDensity = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfirmedRestockingDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfirmedRestockingDetail_SubmittedFlaPropertyCompartment_S~",
                        column: x => x.SubmittedFlaPropertyCompartmentId,
                        principalSchema: "FellingLicenceApplications",
                        principalTable: "SubmittedFlaPropertyCompartment",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ConfirmedFellingSpecies",
                schema: "FellingLicenceApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    ConfirmedFellingDetailId = table.Column<Guid>(type: "uuid", nullable: false),
                    Species = table.Column<string>(type: "text", nullable: false),
                    Percentage = table.Column<double>(type: "double precision", nullable: true),
                    Volume = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfirmedFellingSpecies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfirmedFellingSpecies_ConfirmedFellingDetail_ConfirmedFel~",
                        column: x => x.ConfirmedFellingDetailId,
                        principalSchema: "FellingLicenceApplications",
                        principalTable: "ConfirmedFellingDetail",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConfirmedRestockingSpecies",
                schema: "FellingLicenceApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    ConfirmedRestockingDetailsId = table.Column<Guid>(type: "uuid", nullable: false),
                    Species = table.Column<string>(type: "text", nullable: false),
                    Percentage = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfirmedRestockingSpecies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfirmedRestockingSpecies_ConfirmedRestockingDetail_Confir~",
                        column: x => x.ConfirmedRestockingDetailsId,
                        principalSchema: "FellingLicenceApplications",
                        principalTable: "ConfirmedRestockingDetail",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConfirmedFellingDetail_SubmittedFlaPropertyCompartmentId",
                schema: "FellingLicenceApplications",
                table: "ConfirmedFellingDetail",
                column: "SubmittedFlaPropertyCompartmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfirmedFellingSpecies_ConfirmedFellingDetailId",
                schema: "FellingLicenceApplications",
                table: "ConfirmedFellingSpecies",
                column: "ConfirmedFellingDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfirmedRestockingDetail_SubmittedFlaPropertyCompartmentId",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail",
                column: "SubmittedFlaPropertyCompartmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfirmedRestockingSpecies_ConfirmedRestockingDetailsId",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingSpecies",
                column: "ConfirmedRestockingDetailsId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfirmedFellingSpecies",
                schema: "FellingLicenceApplications");

            migrationBuilder.DropTable(
                name: "ConfirmedRestockingSpecies",
                schema: "FellingLicenceApplications");

            migrationBuilder.DropTable(
                name: "ConfirmedFellingDetail",
                schema: "FellingLicenceApplications");

            migrationBuilder.DropTable(
                name: "ConfirmedRestockingDetail",
                schema: "FellingLicenceApplications");
        }
    }
}
