using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.Applicants.Migrations
{
    public partial class UpdateAgentAuthorityEntities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                schema: "Applicants",
                table: "AgentAuthority");

            migrationBuilder.RenameColumn(
                name: "ApprovalBy",
                schema: "Applicants",
                table: "AgentAuthority",
                newName: "ProcessedByName");

            migrationBuilder.AddColumn<DateTime>(
                name: "ChangedTimestamp",
                schema: "Applicants",
                table: "AgentAuthority",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "Applicants",
                table: "AgentAuthority",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AuthorityConfirmationEmailRecipient",
                schema: "Applicants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    Name = table.Column<string>(type: "text", nullable: false),
                    EmailAddress = table.Column<string>(type: "text", nullable: false),
                    AccessCode = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentAuthorityId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorityConfirmationEmailRecipient", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthorityConfirmationEmailRecipient_AgentAuthority_AgentAut~",
                        column: x => x.AgentAuthorityId,
                        principalSchema: "Applicants",
                        principalTable: "AgentAuthority",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthorityConfirmationEmailRecipient_AgentAuthorityId",
                schema: "Applicants",
                table: "AuthorityConfirmationEmailRecipient",
                column: "AgentAuthorityId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthorityConfirmationEmailRecipient",
                schema: "Applicants");

            migrationBuilder.DropColumn(
                name: "ChangedTimestamp",
                schema: "Applicants",
                table: "AgentAuthority");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "Applicants",
                table: "AgentAuthority");

            migrationBuilder.RenameColumn(
                name: "ProcessedByName",
                schema: "Applicants",
                table: "AgentAuthority",
                newName: "ApprovalBy");

            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                schema: "Applicants",
                table: "AgentAuthority",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
