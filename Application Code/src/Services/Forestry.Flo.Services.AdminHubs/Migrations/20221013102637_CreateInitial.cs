using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.AdminHubs.Migrations
{
    public partial class CreateInitial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "AdminHubs");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

            migrationBuilder.CreateTable(
                name: "AdminHub",
                schema: "AdminHubs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    Name = table.Column<string>(type: "text", nullable: false),
                    AdminManagerId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminHub", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdminHubOfficer",
                schema: "AdminHubs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    UserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminHubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminHubOfficer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminHubOfficer_AdminHub_AdminHubId",
                        column: x => x.AdminHubId,
                        principalSchema: "AdminHubs",
                        principalTable: "AdminHub",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Area",
                schema: "AdminHubs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    AdminHubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Area", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Area_AdminHub_AdminHubId",
                        column: x => x.AdminHubId,
                        principalSchema: "AdminHubs",
                        principalTable: "AdminHub",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminHub_Name",
                schema: "AdminHubs",
                table: "AdminHub",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminHubOfficer_AdminHubId_UserAccountId",
                schema: "AdminHubs",
                table: "AdminHubOfficer",
                columns: new[] { "AdminHubId", "UserAccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Area_AdminHubId",
                schema: "AdminHubs",
                table: "Area",
                column: "AdminHubId");

            migrationBuilder.CreateIndex(
                name: "IX_Area_Code",
                schema: "AdminHubs",
                table: "Area",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Area_Name",
                schema: "AdminHubs",
                table: "Area",
                column: "Name",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminHubOfficer",
                schema: "AdminHubs");

            migrationBuilder.DropTable(
                name: "Area",
                schema: "AdminHubs");

            migrationBuilder.DropTable(
                name: "AdminHub",
                schema: "AdminHubs");
        }
    }
}
