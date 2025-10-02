using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    [ExcludeFromCodeCoverage]
    /// <inheritdoc />
    public partial class AddAttachmentsToConsulteeComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicableToSection",
                schema: "FellingLicenceApplications",
                table: "ConsulteeComment");

            migrationBuilder.AddColumn<string>(
                name: "DocumentIds",
                schema: "FellingLicenceApplications",
                table: "ConsulteeComment",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentIds",
                schema: "FellingLicenceApplications",
                table: "ConsulteeComment");

            migrationBuilder.AddColumn<string>(
                name: "ApplicableToSection",
                schema: "FellingLicenceApplications",
                table: "ConsulteeComment",
                type: "text",
                nullable: true);
        }
    }
}
