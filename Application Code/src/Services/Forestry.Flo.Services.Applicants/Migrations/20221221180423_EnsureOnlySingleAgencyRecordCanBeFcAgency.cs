using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.Applicants.Migrations
{
    public partial class EnsureOnlySingleAgencyRecordCanBeFcAgency : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Agency_IsFcAgency",
                schema: "Applicants",
                table: "Agency",
                column: "IsFcAgency",
                unique: true,
                filter: "\"IsFcAgency\" = true");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Agency_IsFcAgency",
                schema: "Applicants",
                table: "Agency");
        }
    }
}
