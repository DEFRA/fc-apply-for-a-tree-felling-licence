using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.Applicants.Migrations
{
    public partial class RemoveMandatory_ChangedByUserField_OnAgentAuthority : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AgentAuthority_UserAccount_ChangedByUserId",
                schema: "Applicants",
                table: "AgentAuthority");

            migrationBuilder.AlterColumn<Guid>(
                name: "ChangedByUserId",
                schema: "Applicants",
                table: "AgentAuthority",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_AgentAuthority_UserAccount_ChangedByUserId",
                schema: "Applicants",
                table: "AgentAuthority",
                column: "ChangedByUserId",
                principalSchema: "Applicants",
                principalTable: "UserAccount",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AgentAuthority_UserAccount_ChangedByUserId",
                schema: "Applicants",
                table: "AgentAuthority");

            migrationBuilder.AlterColumn<Guid>(
                name: "ChangedByUserId",
                schema: "Applicants",
                table: "AgentAuthority",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentAuthority_UserAccount_ChangedByUserId",
                schema: "Applicants",
                table: "AgentAuthority",
                column: "ChangedByUserId",
                principalSchema: "Applicants",
                principalTable: "UserAccount",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
