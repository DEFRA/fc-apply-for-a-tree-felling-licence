using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.Applicants.Migrations
{
    public partial class AgentAuthority : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentAuthority",
                schema: "Applicants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    CreatedTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalStatus = table.Column<int>(type: "integer", nullable: false),
                    ApprovalBy = table.Column<string>(type: "text", nullable: true),
                    WoodlandOwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgencyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentAuthority", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentAuthority_Agency_AgencyId",
                        column: x => x.AgencyId,
                        principalSchema: "Applicants",
                        principalTable: "Agency",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentAuthority_UserAccount_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "Applicants",
                        principalTable: "UserAccount",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentAuthority_WoodlandOwner_WoodlandOwnerId",
                        column: x => x.WoodlandOwnerId,
                        principalSchema: "Applicants",
                        principalTable: "WoodlandOwner",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentAuthority_AgencyId",
                schema: "Applicants",
                table: "AgentAuthority",
                column: "AgencyId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentAuthority_CreatedByUserId",
                schema: "Applicants",
                table: "AgentAuthority",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentAuthority_WoodlandOwnerId",
                schema: "Applicants",
                table: "AgentAuthority",
                column: "WoodlandOwnerId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentAuthority",
                schema: "Applicants");
        }
    }
}
