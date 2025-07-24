using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.Applicants.Migrations
{
    public partial class UpdateUserAccountEmailAddresses : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Applicants\".\"UserAccount\" SET \"Email\" = LOWER(\"Email\");");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
