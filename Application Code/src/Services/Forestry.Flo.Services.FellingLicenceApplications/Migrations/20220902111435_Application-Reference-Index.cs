using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    public partial class ApplicationReferenceIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_FellingLicenceApplication_ApplicationReference",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplication",
                column: "ApplicationReference",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FellingLicenceApplication_ApplicationReference",
                schema: "FellingLicenceApplications",
                table: "FellingLicenceApplication");
        }
    }
}
