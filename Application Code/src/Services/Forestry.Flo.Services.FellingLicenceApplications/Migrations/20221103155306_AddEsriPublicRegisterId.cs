using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    public partial class AddEsriPublicRegisterId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EsriId",
                schema: "FellingLicenceApplications",
                table: "PublicRegister",
                type: "integer",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EsriId",
                schema: "FellingLicenceApplications",
                table: "PublicRegister");
        }
    }
}
