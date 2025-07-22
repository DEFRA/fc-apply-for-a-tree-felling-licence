using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    public partial class AssignedUserRole : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssigneeHistory_UserAccount_AssignedUserId",
                schema: "FellingLicenceApplications",
                table: "AssigneeHistory");

            migrationBuilder.DropIndex(
                name: "IX_AssigneeHistory_AssignedUserId",
                schema: "FellingLicenceApplications",
                table: "AssigneeHistory");

            migrationBuilder.DropColumn(
                name: "Role",
                schema: "FellingLicenceApplications",
                table: "AssigneeHistory");
            
            migrationBuilder.AddColumn<int>(
                name: "Role",
                schema: "FellingLicenceApplications",
                table: "AssigneeHistory",
                type: "integer",
                nullable: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                schema: "FellingLicenceApplications",
                table: "AssigneeHistory");
            
            migrationBuilder.AddColumn<string>(
                name: "Role",
                schema: "FellingLicenceApplications",
                table: "AssigneeHistory",
                type: "text",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_AssigneeHistory_AssignedUserId",
                schema: "FellingLicenceApplications",
                table: "AssigneeHistory",
                column: "AssignedUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AssigneeHistory_UserAccount_AssignedUserId",
                schema: "FellingLicenceApplications",
                table: "AssigneeHistory",
                column: "AssignedUserId",
                principalSchema: "Applicants",
                principalTable: "UserAccount",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
