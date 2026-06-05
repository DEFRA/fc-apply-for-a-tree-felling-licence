using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    [ExcludeFromCodeCoverage]
    /// <inheritdoc />
    public partial class CorrectPhytophthoraSpelling : Migration
    {
        private const string UpSql = @"
UPDATE ""FellingLicenceApplications"".""FellingLicenceApplication""
SET ""TreeHealthIssues"" = REPLACE(""TreeHealthIssues"", 'Phytophora', 'Phytophthora');
";

        private const string DownSql = @"
UPDATE ""FellingLicenceApplications"".""FellingLicenceApplication""
SET ""TreeHealthIssues"" = REPLACE(""TreeHealthIssues"", 'Phytophthora', 'Phytophora');
";
        
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(UpSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DownSql);
        }
    }
}
