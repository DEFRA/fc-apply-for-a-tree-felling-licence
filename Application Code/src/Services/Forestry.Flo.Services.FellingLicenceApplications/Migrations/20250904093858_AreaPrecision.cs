using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    public partial class AreaPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "TotalHectares",
                schema: "FellingLicenceApplications",
                table: "SubmittedFlaPropertyCompartment",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "ConfirmedTotalHectares",
                schema: "FellingLicenceApplications",
                table: "SubmittedFlaPropertyCompartment",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "PercentageOfRestockArea",
                schema: "FellingLicenceApplications",
                table: "ProposedRestockingDetail",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Area",
                schema: "FellingLicenceApplications",
                table: "ProposedRestockingDetail",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "AreaToBeFelled",
                schema: "FellingLicenceApplications",
                table: "ProposedFellingDetail",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AlterColumn<double>(
                name: "PercentageOfRestockArea",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Area",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "AreaToBeFelled",
                schema: "FellingLicenceApplications",
                table: "ConfirmedFellingDetail",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "TotalHectares",
                schema: "FellingLicenceApplications",
                table: "SubmittedFlaPropertyCompartment",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "ConfirmedTotalHectares",
                schema: "FellingLicenceApplications",
                table: "SubmittedFlaPropertyCompartment",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "PercentageOfRestockArea",
                schema: "FellingLicenceApplications",
                table: "ProposedRestockingDetail",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Area",
                schema: "FellingLicenceApplications",
                table: "ProposedRestockingDetail",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "AreaToBeFelled",
                schema: "FellingLicenceApplications",
                table: "ProposedFellingDetail",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<double>(
                name: "PercentageOfRestockArea",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Area",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "AreaToBeFelled",
                schema: "FellingLicenceApplications",
                table: "ConfirmedFellingDetail",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);
        }
    }
}
