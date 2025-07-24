using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.InternalUsers.Migrations
{
    public partial class ReplaceConfirmedBoolWithAccountStatusEnum : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                schema: "InternalUsers",
                table: "UserAccount",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            // Set all previously confirmed accounts to confirmed

            migrationBuilder.Sql("UPDATE \"InternalUsers\".\"UserAccount\" SET \"Status\" = 0 WHERE \"UserAccountConfirmed\" = true;", true);

            migrationBuilder.DropColumn(
                name: "UserAccountConfirmationDenied",
                schema: "InternalUsers",
                table: "UserAccount");

            migrationBuilder.DropColumn(
                name: "UserAccountConfirmed",
                schema: "InternalUsers",
                table: "UserAccount");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                schema: "InternalUsers",
                table: "UserAccount");

            migrationBuilder.AddColumn<bool>(
                name: "UserAccountConfirmationDenied",
                schema: "InternalUsers",
                table: "UserAccount",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UserAccountConfirmed",
                schema: "InternalUsers",
                table: "UserAccount",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
