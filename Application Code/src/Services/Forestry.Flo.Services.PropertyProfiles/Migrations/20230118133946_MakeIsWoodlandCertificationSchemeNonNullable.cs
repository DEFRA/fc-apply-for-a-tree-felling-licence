using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Forestry.Flo.Services.PropertyProfiles.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class MakeIsWoodlandCertificationSchemeNonNullable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Set IsWoodlandCertificationScheme to false for null values to avoid compatibility issues

            migrationBuilder.Sql("UPDATE \"PropertyProfiles\".\"PropertyProfile\" SET \"IsWoodlandCertificationScheme\" = false WHERE \"IsWoodlandCertificationScheme\" is null;", true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsWoodlandCertificationScheme",
                schema: "PropertyProfiles",
                table: "PropertyProfile",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsWoodlandCertificationScheme",
                schema: "PropertyProfiles",
                table: "PropertyProfile",
                type: "boolean",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "boolean");
        }
    }
}
