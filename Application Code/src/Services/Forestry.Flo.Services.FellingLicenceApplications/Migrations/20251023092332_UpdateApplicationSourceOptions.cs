using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class UpdateApplicationSourceOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE \"FellingLicenceApplications\".\"FellingLicenceApplication\" SET \"Source\" = 'DigitalAssistance' WHERE \"Source\" = 'WoodlandManagementPlan';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE \"FellingLicenceApplications\".\"FellingLicenceApplication\" SET \"Source\" = 'WoodlandManagementPlan' WHERE \"Source\" = 'DigitalAssistance';");
        }
    }
}
