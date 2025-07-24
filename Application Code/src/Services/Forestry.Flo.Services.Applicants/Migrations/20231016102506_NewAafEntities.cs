using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.Applicants.Migrations
{
    public partial class NewAafEntities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthorityConfirmationEmailRecipient",
                schema: "Applicants");

            migrationBuilder.DropColumn(
                name: "ApproverSignature",
                schema: "Applicants",
                table: "AgentAuthority");

            migrationBuilder.DropColumn(
                name: "ProcessedByName",
                schema: "Applicants",
                table: "AgentAuthority");

            migrationBuilder.AddColumn<Guid>(
                name: "ChangedByUserId",
                schema: "Applicants",
                table: "AgentAuthority",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "AgentAuthorityForm",
                schema: "Applicants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    UploadedById = table.Column<Guid>(type: "uuid", nullable: false),
                    ValidFromDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidToDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AgentAuthorityId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentAuthorityForm", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentAuthorityForm_AgentAuthority_AgentAuthorityId",
                        column: x => x.AgentAuthorityId,
                        principalSchema: "Applicants",
                        principalTable: "AgentAuthority",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentAuthorityForm_UserAccount_UploadedById",
                        column: x => x.UploadedById,
                        principalSchema: "Applicants",
                        principalTable: "UserAccount",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AafDocument",
                schema: "Applicants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    AgentAuthorityFormId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    FileType = table.Column<string>(type: "text", nullable: false),
                    MimeType = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AafDocument", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AafDocument_AgentAuthorityForm_AgentAuthorityFormId",
                        column: x => x.AgentAuthorityFormId,
                        principalSchema: "Applicants",
                        principalTable: "AgentAuthorityForm",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentAuthority_ChangedByUserId",
                schema: "Applicants",
                table: "AgentAuthority",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AafDocument_AgentAuthorityFormId",
                schema: "Applicants",
                table: "AafDocument",
                column: "AgentAuthorityFormId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentAuthorityForm_AgentAuthorityId",
                schema: "Applicants",
                table: "AgentAuthorityForm",
                column: "AgentAuthorityId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentAuthorityForm_UploadedById",
                schema: "Applicants",
                table: "AgentAuthorityForm",
                column: "UploadedById");

            migrationBuilder.AddForeignKey(
                name: "FK_AgentAuthority_UserAccount_ChangedByUserId",
                schema: "Applicants",
                table: "AgentAuthority",
                column: "ChangedByUserId",
                principalSchema: "Applicants",
                principalTable: "UserAccount",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AgentAuthority_UserAccount_ChangedByUserId",
                schema: "Applicants",
                table: "AgentAuthority");

            migrationBuilder.DropTable(
                name: "AafDocument",
                schema: "Applicants");

            migrationBuilder.DropTable(
                name: "AgentAuthorityForm",
                schema: "Applicants");

            migrationBuilder.DropIndex(
                name: "IX_AgentAuthority_ChangedByUserId",
                schema: "Applicants",
                table: "AgentAuthority");

            migrationBuilder.DropColumn(
                name: "ChangedByUserId",
                schema: "Applicants",
                table: "AgentAuthority");

            migrationBuilder.AddColumn<string>(
                name: "ApproverSignature",
                schema: "Applicants",
                table: "AgentAuthority",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessedByName",
                schema: "Applicants",
                table: "AgentAuthority",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuthorityConfirmationEmailRecipient",
                schema: "Applicants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    AccessCode = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentAuthorityId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailAddress = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorityConfirmationEmailRecipient", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthorityConfirmationEmailRecipient_AgentAuthority_AgentAut~",
                        column: x => x.AgentAuthorityId,
                        principalSchema: "Applicants",
                        principalTable: "AgentAuthority",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthorityConfirmationEmailRecipient_AgentAuthorityId",
                schema: "Applicants",
                table: "AuthorityConfirmationEmailRecipient",
                column: "AgentAuthorityId");
        }
    }
}
