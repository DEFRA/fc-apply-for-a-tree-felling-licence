using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Forestry.Flo.Services.InternalUsers.Migrations
{
    [ExcludeFromCodeCoverage]
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
