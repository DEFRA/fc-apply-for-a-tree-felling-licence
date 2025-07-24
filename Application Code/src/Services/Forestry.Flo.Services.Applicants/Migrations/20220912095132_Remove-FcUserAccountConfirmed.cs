using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.Applicants.Migrations
{
    public partial class RemoveFcUserAccountConfirmed : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FcUserAccountConfirmed",
                schema: "Applicants",
                table: "UserAccount");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FcUserAccountConfirmed",
                schema: "Applicants",
                table: "UserAccount",
                type: "boolean",
                nullable: true);
        }
    }
}
