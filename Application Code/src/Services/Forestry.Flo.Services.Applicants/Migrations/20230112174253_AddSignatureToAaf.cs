using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Forestry.Flo.Services.Applicants.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class AddSignatureToAaf : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApproverSignature",
                schema: "Applicants",
                table: "AgentAuthority",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApproverSignature",
                schema: "Applicants",
                table: "AgentAuthority");
        }
    }
}
