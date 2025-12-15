using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    public partial class AddPawsToSubmittedDesignations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasBeenReviewed",
                schema: "FellingLicenceApplications",
                table: "SubmittedCompartmentDesignations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Paws",
                schema: "FellingLicenceApplications",
                table: "SubmittedCompartmentDesignations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProportionAfterFelling",
                schema: "FellingLicenceApplications",
                table: "SubmittedCompartmentDesignations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProportionBeforeFelling",
                schema: "FellingLicenceApplications",
                table: "SubmittedCompartmentDesignations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProposedCompartmentDesignationsId",
                schema: "FellingLicenceApplications",
                table: "SubmittedCompartmentDesignations",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasBeenReviewed",
                schema: "FellingLicenceApplications",
                table: "SubmittedCompartmentDesignations");

            migrationBuilder.DropColumn(
                name: "Paws",
                schema: "FellingLicenceApplications",
                table: "SubmittedCompartmentDesignations");

            migrationBuilder.DropColumn(
                name: "ProportionAfterFelling",
                schema: "FellingLicenceApplications",
                table: "SubmittedCompartmentDesignations");

            migrationBuilder.DropColumn(
                name: "ProportionBeforeFelling",
                schema: "FellingLicenceApplications",
                table: "SubmittedCompartmentDesignations");

            migrationBuilder.DropColumn(
                name: "ProposedCompartmentDesignationsId",
                schema: "FellingLicenceApplications",
                table: "SubmittedCompartmentDesignations");
        }
    }
}
