using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Forestry.Flo.Services.InternalUsers.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class AddAccountTypeOther : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountTypeOther",
                schema: "InternalUsers",
                table: "UserAccount",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountTypeOther",
                schema: "InternalUsers",
                table: "UserAccount");
        }
    }
}
