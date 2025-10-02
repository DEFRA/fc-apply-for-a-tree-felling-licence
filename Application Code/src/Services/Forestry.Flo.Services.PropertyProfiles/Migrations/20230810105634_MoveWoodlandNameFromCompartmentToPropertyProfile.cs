using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Forestry.Flo.Services.PropertyProfiles.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class MoveWoodlandNameFromCompartmentToPropertyProfile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WoodlandName",
                schema: "PropertyProfiles",
                table: "Compartment");

            migrationBuilder.AddColumn<string>(
                name: "NameOfWood",
                schema: "PropertyProfiles",
                table: "PropertyProfile",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NameOfWood",
                schema: "PropertyProfiles",
                table: "PropertyProfile");

            migrationBuilder.AddColumn<string>(
                name: "WoodlandName",
                schema: "PropertyProfiles",
                table: "Compartment",
                type: "text",
                nullable: true);
        }
    }
}
