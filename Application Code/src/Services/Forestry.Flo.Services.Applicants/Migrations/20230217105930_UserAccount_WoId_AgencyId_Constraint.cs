using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.Applicants.Migrations
{
    public partial class UserAccount_WoId_AgencyId_Constraint : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE \"Applicants\".\"UserAccount\" SET \"WoodlandOwnerId\" = NULL WHERE \"AccountType\" IN (2,3,4);");

            migrationBuilder.Sql(
                "UPDATE \"Applicants\".\"UserAccount\" SET \"AgencyId\" = NULL WHERE \"AccountType\" IN (0,1);");

            migrationBuilder.Sql(
                "ALTER TABLE \"Applicants\".\"UserAccount\" ADD CONSTRAINT CannotHaveWoodlandOwnerIdAndAgencyId CHECK ((\"WoodlandOwnerId\" IS NULL AND \"AgencyId\" IS NOT NULL) OR (\"WoodlandOwnerId\" IS NOT NULL AND \"AgencyId\" IS NULL) OR (\"WoodlandOwnerId\" IS NULL AND \"AgencyId\" IS NULL));");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"Applicants\".\"UserAccount\" DROP CONSTRAINT CannotHaveWoodlandOwnerIdAndAgencyId");
        }
    }
}
