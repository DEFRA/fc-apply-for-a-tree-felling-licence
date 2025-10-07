using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class FellingLicenceApplicationStepStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FellingLicenceApplicationStepStatus",
                schema: "FellingLicenceApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    ApplicationDetailsStatus = table.Column<bool>(type: "boolean", nullable: true),
                    SelectCompartmentsStatus = table.Column<bool>(type: "boolean", nullable: true),
                    OperationsStatus = table.Column<bool>(type: "boolean", nullable: true),
                    SupportingDocumentationStatus = table.Column<bool>(type: "boolean", nullable: true),
                    TermsAndConditionsStatus = table.Column<bool>(type: "boolean", nullable: true),
                    CompartmentFellingRestockingStatuses = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FellingLicenceApplicationStepStatus", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FellingLicenceApplicationStepStatus",
                schema: "FellingLicenceApplications");
        }
    }
}
