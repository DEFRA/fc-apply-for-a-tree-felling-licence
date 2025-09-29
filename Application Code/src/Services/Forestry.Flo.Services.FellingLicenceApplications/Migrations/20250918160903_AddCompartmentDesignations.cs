using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    public partial class AddCompartmentDesignations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DesignationsComplete",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "SubmittedCompartmentDesignations",
                schema: "FellingLicenceApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    SubmittedFlaPropertyCompartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sssi = table.Column<bool>(type: "boolean", nullable: false),
                    Sacs = table.Column<bool>(type: "boolean", nullable: false),
                    Spa = table.Column<bool>(type: "boolean", nullable: false),
                    Ramser = table.Column<bool>(type: "boolean", nullable: false),
                    Sbi = table.Column<bool>(type: "boolean", nullable: false),
                    Other = table.Column<bool>(type: "boolean", nullable: false),
                    OtherDesignationDetails = table.Column<string>(type: "text", nullable: true),
                    None = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmittedCompartmentDesignations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmittedCompartmentDesignations_SubmittedFlaPropertyCompar~",
                        column: x => x.SubmittedFlaPropertyCompartmentId,
                        principalSchema: "FellingLicenceApplications",
                        principalTable: "SubmittedFlaPropertyCompartment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedCompartmentDesignations_SubmittedFlaPropertyCompar~",
                schema: "FellingLicenceApplications",
                table: "SubmittedCompartmentDesignations",
                column: "SubmittedFlaPropertyCompartmentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubmittedCompartmentDesignations",
                schema: "FellingLicenceApplications");

            migrationBuilder.DropColumn(
                name: "DesignationsComplete",
                schema: "FellingLicenceApplications",
                table: "WoodlandOfficerReview");
        }
    }
}
