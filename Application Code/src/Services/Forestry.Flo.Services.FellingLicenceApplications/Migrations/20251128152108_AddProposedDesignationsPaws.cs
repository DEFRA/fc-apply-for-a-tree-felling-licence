using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    public partial class AddProposedDesignationsPaws : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompartmentDesignationsStatuses",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplicationStepStatus",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ProposedCompartmentDesignations",
                schema: "FellingLicenceApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    LinkedPropertyProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyProfileCompartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CrossesPawsZones = table.Column<int>(type: "integer", nullable: true),
                    ProportionBeforeFelling = table.Column<string>(type: "text", nullable: true),
                    ProportionAfterFelling = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProposedCompartmentDesignations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProposedCompartmentDesignations_LinkedPropertyProfile_Linke~",
                        column: x => x.LinkedPropertyProfileId,
                        principalSchema: "FellingLicenceApplications",
                        principalTable: "LinkedPropertyProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProposedCompartmentDesignations_LinkedPropertyProfileId",
                schema: "FellingLicenceApplications",
                table: "ProposedCompartmentDesignations",
                column: "LinkedPropertyProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProposedCompartmentDesignations",
                schema: "FellingLicenceApplications");

            migrationBuilder.DropColumn(
                name: "CompartmentDesignationsStatuses",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplicationStepStatus");
        }
    }
}
