using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    /// <inheritdoc />
    public partial class AccessCodeOnConsulteeComment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AccessCode",
                schema: "FellingLicenceApplications",
                table: "ConsulteeComment",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql(
                "UPDATE \"FellingLicenceApplications\".\"ConsulteeComment\" AS c SET \"AccessCode\" = l.\"AccessCode\" FROM \"FellingLicenceApplications\".\"ExternalAccessLink\" AS l WHERE l.\"ContactEmail\" = c.\"AuthorContactEmail\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessCode",
                schema: "FellingLicenceApplications",
                table: "ConsulteeComment");
        }
    }
}
