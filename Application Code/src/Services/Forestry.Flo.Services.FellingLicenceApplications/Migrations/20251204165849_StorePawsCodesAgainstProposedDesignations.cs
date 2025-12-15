using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    public partial class StorePawsCodesAgainstProposedDesignations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CrossesPawsZones",
                schema: "FellingLicenceApplications",
                table: "ProposedCompartmentDesignations",
                type: "text",
                nullable: false,
                defaultValue: "[]",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.Sql(
                @"UPDATE ""FellingLicenceApplications"".""ProposedCompartmentDesignations"" 
                  SET ""CrossesPawsZones"" = '[]'
                  WHERE ""CrossesPawsZones"" = '0';");

            migrationBuilder.Sql(
                @"UPDATE ""FellingLicenceApplications"".""ProposedCompartmentDesignations"" 
                  SET ""CrossesPawsZones"" = '[""ARW""]'
                  WHERE ""CrossesPawsZones"" = '1';");

            migrationBuilder.Sql(
                @"UPDATE ""FellingLicenceApplications"".""ProposedCompartmentDesignations"" 
                  SET ""CrossesPawsZones"" = '[""ARW"", ""IAWPP""]'
                  WHERE ""CrossesPawsZones"" = '2';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "CrossesPawsZones",
                schema: "FellingLicenceApplications",
                table: "ProposedCompartmentDesignations",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
