using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.Applicants.Migrations
{
    public partial class Agency : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AgencyId",
                schema: "Applicants",
                table: "UserAccount",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Agency",
                schema: "Applicants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    Address_Line1 = table.Column<string>(type: "text", nullable: true),
                    Address_Line2 = table.Column<string>(type: "text", nullable: true),
                    Address_Line3 = table.Column<string>(type: "text", nullable: true),
                    Address_Line4 = table.Column<string>(type: "text", nullable: true),
                    Address_PostalCode = table.Column<string>(type: "text", nullable: true),
                    ContactEmail = table.Column<string>(type: "text", nullable: true),
                    ContactName = table.Column<string>(type: "text", nullable: true),
                    OrganisationName = table.Column<string>(type: "text", nullable: true),
                    ShouldAutoApproveThinningApplications = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agency", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserAccount_AgencyId",
                schema: "Applicants",
                table: "UserAccount",
                column: "AgencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserAccount_Agency_AgencyId",
                schema: "Applicants",
                table: "UserAccount",
                column: "AgencyId",
                principalSchema: "Applicants",
                principalTable: "Agency",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserAccount_Agency_AgencyId",
                schema: "Applicants",
                table: "UserAccount");

            migrationBuilder.DropTable(
                name: "Agency",
                schema: "Applicants");

            migrationBuilder.DropIndex(
                name: "IX_UserAccount_AgencyId",
                schema: "Applicants",
                table: "UserAccount");

            migrationBuilder.DropColumn(
                name: "AgencyId",
                schema: "Applicants",
                table: "UserAccount");
        }
    }
}
