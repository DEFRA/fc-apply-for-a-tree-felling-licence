using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    public partial class DefaultExistingRestockingRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"UPDATE ""FellingLicenceApplications"".""ProposedRestockingDetail"" 
                  SET ""PercentageEstablishedByCoppiceOrNaturalRegen"" = 100
                  WHERE ""PercentageEstablishedByCoppiceOrNaturalRegen"" IS NULL AND (""RestockingProposal"" = 'RestockWithCoppiceRegrowth' OR ""RestockingProposal"" = 'RestockByNaturalRegeneration');");

            migrationBuilder.Sql(
                @"UPDATE ""FellingLicenceApplications"".""ConfirmedRestockingDetail"" 
                  SET ""PercentageEstablishedByCoppiceOrNaturalRegen"" = 100
                  WHERE ""PercentageEstablishedByCoppiceOrNaturalRegen"" IS NULL AND (""RestockingProposal"" = 7 OR ""RestockingProposal"" = 8);");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
