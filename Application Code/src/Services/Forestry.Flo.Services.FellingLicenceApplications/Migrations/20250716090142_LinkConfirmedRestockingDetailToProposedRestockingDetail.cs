using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    [ExcludeFromCodeCoverage]
    /// <inheritdoc />
    public partial class LinkConfirmedRestockingDetailToProposedRestockingDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProposedRestockingDetailId",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProposedRestockingDetailId",
                schema: "FellingLicenceApplications",
                table: "ConfirmedRestockingDetail");
        }
    }
}
