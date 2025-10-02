using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    [ExcludeFromCodeCoverage]
    /// <inheritdoc />
    public partial class FLOV2074AddNumberOfTrees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NumberOfTrees",
                schema: "FellingLicenceApplications",
                table: "ProposedRestockingDetail",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "FlightDate",
                schema: "FellingLicenceApplications",
                table: "LarchCheckDetails",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfTrees",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumberOfTrees",
                schema: "FellingLicenceApplications",
                table: "ProposedRestockingDetail");

            migrationBuilder.DropColumn(
                name: "NumberOfTrees",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "FlightDate",
                schema: "FellingLicenceApplications",
                table: "LarchCheckDetails",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
