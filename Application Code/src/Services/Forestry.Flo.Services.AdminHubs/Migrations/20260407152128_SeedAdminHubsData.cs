using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Forestry.Flo.Services.AdminHubs.Migrations
{
    [ExcludeFromCodeCoverage]
    /// <inheritdoc />
    public partial class SeedAdminHubsData : Migration
    {
        private const string SeedAdminHub =
            "INSERT INTO \"AdminHubs\".\"AdminHub\"(\"Id\", \"Name\", \"AdminManagerId\", \"Address\")\r\nVALUES('[ID]', '[NAME]', '[MANAGERID]', '[ADDRESS]')\r\nON CONFLICT (\"Id\") DO NOTHING;";

        private const string SeedArea =
            "INSERT INTO \"AdminHubs\".\"Area\"(\"Id\", \"Name\", \"Code\", \"AdminHubId\")\r\nVALUES('[ID]', '[NAME]', '[CODE]', '[ADMINHUBID]')\r\nON CONFLICT (\"Id\") DO NOTHING;";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(SeedAdminHub
                .Replace("[ID]", "1A394600-23CD-4EAE-89CC-C47890AD4047")
                .Replace("[NAME]", "Bucks Horn Oak")
                .Replace("[MANAGERID]", "4927368A-CCD0-4D46-9BF0-468CC5526B70")
                .Replace("[ADDRESS]", "Bucks Horn Oak\\nFarnham\\nSurrey\\nGU10 4LS\\nPhone: 0300 067 4420\\nEmail: adminhub.buckshornoak@forestrycommission.gov.uk"));

            migrationBuilder.Sql(SeedAdminHub
                .Replace("[ID]", "44F20062-E77F-4C1C-BD47-0CC30DD965E9")
                .Replace("[NAME]", "Bullers Hill")
                .Replace("[MANAGERID]", "0DB0775E-87B4-48FF-889D-C7A7B82C093D")
                .Replace("[ADDRESS]", "Bullers Hill\\nKennford\\nExeter\\nEX6 7XR\\nPhone: 0300 067 4960\\nEmail: adminhub.bullershill@forestrycommission.gov.uk"));

            migrationBuilder.Sql("DELETE FROM \"AdminHubs\".\"Area\";");

            migrationBuilder.Sql(SeedArea
                .Replace("[ID]", "8BC3F047-DA34-493C-9A2D-5A4926A482F9")
                .Replace("[NAME]", "North West & West Midlands")
                .Replace("[CODE]", "010")
                .Replace("[ADMINHUBID]", "44F20062-E77F-4C1C-BD47-0CC30DD965E9"));

            migrationBuilder.Sql(SeedArea
                .Replace("[ID]", "0DB4755F-A80D-4068-9D2D-BC3D950EAB76")
                .Replace("[NAME]", "East & East Midlands")
                .Replace("[CODE]", "017")
                .Replace("[ADMINHUBID]", "1A394600-23CD-4EAE-89CC-C47890AD4047"));

            migrationBuilder.Sql(SeedArea
                .Replace("[ID]", "983C2D0B-C56B-4B65-99EA-5B14C00545C4")
                .Replace("[NAME]", "South West")
                .Replace("[CODE]", "018")
                .Replace("[ADMINHUBID]", "44F20062-E77F-4C1C-BD47-0CC30DD965E9"));

            migrationBuilder.Sql(SeedArea
                .Replace("[ID]", "5A1C130E-A671-4CCC-9112-1C41CBFF181D")
                .Replace("[NAME]", "South East & London")
                .Replace("[CODE]", "019")
                .Replace("[ADMINHUBID]", "1A394600-23CD-4EAE-89CC-C47890AD4047"));

            migrationBuilder.Sql(SeedArea
                .Replace("[ID]", "4DA35A0F-9CB3-4C55-829F-9589F04BF790")
                .Replace("[NAME]", "Yorkshire & North East")
                .Replace("[CODE]", "022")
                .Replace("[ADMINHUBID]", "1A394600-23CD-4EAE-89CC-C47890AD4047"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
