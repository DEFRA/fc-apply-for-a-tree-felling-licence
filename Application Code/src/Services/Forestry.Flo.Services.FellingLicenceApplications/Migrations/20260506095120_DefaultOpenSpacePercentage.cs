using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    [ExcludeFromCodeCoverage]
    /// <inheritdoc />
    public partial class DefaultOpenSpacePercentage : Migration
    {
        private const string ProposedSql = @"UPDATE ""FellingLicenceApplications"".""ProposedRestockingDetail""
	SET ""PercentOpenSpace"" = 0
	WHERE ""PercentOpenSpace"" IS NULL 
	AND ""RestockingProposal"" IS NOT NULL 
	AND ""RestockingProposal"" != 'None' 
	AND ""RestockingProposal"" != 'CreateDesignedOpenGround' 
	AND ""RestockingProposal"" != 'DoNotIntendToRestock';";

        private const string ConfirmedSql = @"UPDATE ""FellingLicenceApplications"".""ConfirmedRestockingDetail""
	SET ""PercentOpenSpace"" = 0
	WHERE ""PercentOpenSpace"" IS NULL 
	AND ""RestockingProposal"" IS NOT NULL 
	AND ""RestockingProposal"" > 2;";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(ProposedSql);

            migrationBuilder.Sql(ConfirmedSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // no Down migration as we cannot revert the update to null values
        }
    }
}
