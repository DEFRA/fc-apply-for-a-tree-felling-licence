using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    public partial class AddPublicRegister : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PublicRegister",
                schema: "FellingLicenceApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    FellingLicenceApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExemptFromPublicRegister = table.Column<bool>(type: "boolean", nullable: false),
                    ExemptionReason = table.Column<string>(type: "text", nullable: true),
                    PublicationTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiryTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RemovedTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicRegister", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PublicRegister_FellingLicenceApplication_FellingLicenceAppl~",
                        column: x => x.FellingLicenceApplicationId,
                        principalSchema: "FellingLicenceApplications",
                        principalTable: "FellingLicenceApplication",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PublicRegister_FellingLicenceApplicationId",
                schema: "FellingLicenceApplications",
                table: "PublicRegister",
                column: "FellingLicenceApplicationId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PublicRegister",
                schema: "FellingLicenceApplications");
        }
    }
}
