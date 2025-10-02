using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Forestry.Flo.Services.Applicants.Migrations
{
    [ExcludeFromCodeCoverage]
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
