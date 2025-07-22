using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.Applicants.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Applicants");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

            migrationBuilder.CreateTable(
                name: "WoodlandOwner",
                schema: "Applicants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    ContactAddress_Line1 = table.Column<string>(type: "text", nullable: true),
                    ContactAddress_Line2 = table.Column<string>(type: "text", nullable: true),
                    ContactAddress_Line3 = table.Column<string>(type: "text", nullable: true),
                    ContactAddress_Line4 = table.Column<string>(type: "text", nullable: true),
                    ContactAddress_PostalCode = table.Column<string>(type: "text", nullable: true),
                    ContactEmail = table.Column<string>(type: "text", nullable: true),
                    ContactName = table.Column<string>(type: "text", nullable: true),
                    IsOrganisation = table.Column<bool>(type: "boolean", nullable: false),
                    OrganisationName = table.Column<string>(type: "text", nullable: true),
                    OrganisationAddress_Line1 = table.Column<string>(type: "text", nullable: true),
                    OrganisationAddress_Line2 = table.Column<string>(type: "text", nullable: true),
                    OrganisationAddress_Line3 = table.Column<string>(type: "text", nullable: true),
                    OrganisationAddress_Line4 = table.Column<string>(type: "text", nullable: true),
                    OrganisationAddress_PostalCode = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WoodlandOwner", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserAccount",
                schema: "Applicants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    IdentityProviderId = table.Column<string>(type: "text", nullable: true),
                    AccountType = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    FirstName = table.Column<string>(type: "text", nullable: true),
                    LastName = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PreferredContactMethod = table.Column<string>(type: "text", nullable: true),
                    ContactAddress_Line1 = table.Column<string>(type: "text", nullable: true),
                    ContactAddress_Line2 = table.Column<string>(type: "text", nullable: true),
                    ContactAddress_Line3 = table.Column<string>(type: "text", nullable: true),
                    ContactAddress_Line4 = table.Column<string>(type: "text", nullable: true),
                    ContactAddress_PostalCode = table.Column<string>(type: "text", nullable: true),
                    ContactTelephone = table.Column<string>(type: "text", nullable: true),
                    ContactMobileTelephone = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    DateAcceptedTermsAndConditions = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DateAcceptedPrivacyPolicy = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InviteToken = table.Column<Guid>(type: "uuid", nullable: true),
                    InviteTokenExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WoodlandOwnerId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccount", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAccount_WoodlandOwner_WoodlandOwnerId",
                        column: x => x.WoodlandOwnerId,
                        principalSchema: "Applicants",
                        principalTable: "WoodlandOwner",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserAccount_Email",
                schema: "Applicants",
                table: "UserAccount",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAccount_IdentityProviderId",
                schema: "Applicants",
                table: "UserAccount",
                column: "IdentityProviderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAccount_WoodlandOwnerId",
                schema: "Applicants",
                table: "UserAccount",
                column: "WoodlandOwnerId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserAccount",
                schema: "Applicants");

            migrationBuilder.DropTable(
                name: "WoodlandOwner",
                schema: "Applicants");
        }
    }
}
