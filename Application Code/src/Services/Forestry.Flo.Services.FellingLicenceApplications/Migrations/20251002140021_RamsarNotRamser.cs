using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    [ExcludeFromCodeCoverage]
    /// <inheritdoc />
    public partial class RamsarNotRamser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Ramser",
                schema: "FellingLicenceApplications",
                table: "SubmittedCompartmentDesignations",
                newName: "Ramsar");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Ramsar",
                schema: "FellingLicenceApplications",
                table: "SubmittedCompartmentDesignations",
                newName: "Ramser");
        }
    }
}
