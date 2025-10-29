using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class FellingLicenceApplicationStepStatus_ManualRows : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            string sql = @"

                TRUNCATE TABLE ""FellingLicenceApplications"".""FellingLicenceApplicationStepStatus"";

                INSERT INTO ""FellingLicenceApplications"".""FellingLicenceApplicationStepStatus""(""Id"", ""FellingLicenceApplicationId"", ""CompartmentFellingRestockingStatuses"")
                SELECT uuid_in(overlay(overlay(md5(random()::text || ':' || random()::text) placing '4' from 13) placing to_hex(floor(random()*(11 - 8 + 1) +8)::int)::text from 17)::cstring),
                ""Id"", '[]'
                FROM ""FellingLicenceApplications"".""FellingLicenceApplication"" fla;

            ";

            migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            string sql = @"

                TRUNCATE TABLE ""FellingLicenceApplications"".""FellingLicenceApplicationStepStatus"";
            ";

            migrationBuilder.Sql(sql);
        }
    }
}
