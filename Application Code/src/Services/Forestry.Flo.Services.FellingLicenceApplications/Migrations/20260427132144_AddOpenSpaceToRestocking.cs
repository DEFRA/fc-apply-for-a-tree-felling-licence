using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    [ExcludeFromCodeCoverage]
    /// <inheritdoc />
    public partial class AddOpenSpaceToRestocking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "PercentOpenSpace",
                schema: "FellingLicenceApplications",
                table: "ProposedRestockingDetail",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "PercentOpenSpace",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PercentOpenSpace",
                schema: "FellingLicenceApplications",
                table: "ProposedRestockingDetail");

            migrationBuilder.AlterColumn<int>(
                name: "PercentOpenSpace",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail",
                type: "integer",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);
        }
    }
}
