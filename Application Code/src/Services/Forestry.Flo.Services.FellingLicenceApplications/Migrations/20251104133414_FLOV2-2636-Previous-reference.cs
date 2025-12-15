using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    public partial class FLOV22636Previousreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OldReferenceNumber",
                schema: "FellingLicenceApplications",
                table: "ApprovedInError",
                newName: "PreviousReference");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PreviousReference",
                schema: "FellingLicenceApplications",
                table: "ApprovedInError",
                newName: "OldReferenceNumber");
        }
    }
}
