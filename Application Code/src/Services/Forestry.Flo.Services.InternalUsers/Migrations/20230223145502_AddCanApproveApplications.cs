using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.InternalUsers.Migrations
{
    public partial class AddCanApproveApplications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanApproveApplications",
                schema: "InternalUsers",
                table: "UserAccount",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanApproveApplications",
                schema: "InternalUsers",
                table: "UserAccount");
        }
    }
}
