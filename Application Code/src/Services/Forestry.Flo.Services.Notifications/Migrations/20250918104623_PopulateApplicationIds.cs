using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.Notifications.Migrations
{
    /// <inheritdoc />
    public partial class PopulateApplicationIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE \"Notifications\".\"NotificationHistory\" n SET \"ApplicationId\" = a.\"Id\" FROM \"FellingLicenceApplications\".\"FellingLicenceApplication\" a WHERE n.\"ApplicationReference\" = a.\"ApplicationReference\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
