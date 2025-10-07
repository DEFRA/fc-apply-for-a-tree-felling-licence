using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Forestry.Flo.Services.Applicants.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class AddedNewAccountTypes_WithWoodlandOwnerAdditions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LandlordFirstName",
                schema: "Applicants",
                table: "WoodlandOwner",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LandlordLastName",
                schema: "Applicants",
                table: "WoodlandOwner",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantType",
                schema: "Applicants",
                table: "WoodlandOwner",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WoodlandOwnerType",
                schema: "Applicants",
                table: "WoodlandOwner",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsOrganisation",
                schema: "Applicants",
                table: "Agency",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LandlordFirstName",
                schema: "Applicants",
                table: "WoodlandOwner");

            migrationBuilder.DropColumn(
                name: "LandlordLastName",
                schema: "Applicants",
                table: "WoodlandOwner");

            migrationBuilder.DropColumn(
                name: "TenantType",
                schema: "Applicants",
                table: "WoodlandOwner");

            migrationBuilder.DropColumn(
                name: "WoodlandOwnerType",
                schema: "Applicants",
                table: "WoodlandOwner");

            migrationBuilder.DropColumn(
                name: "IsOrganisation",
                schema: "Applicants",
                table: "Agency");
        }
    }
}
