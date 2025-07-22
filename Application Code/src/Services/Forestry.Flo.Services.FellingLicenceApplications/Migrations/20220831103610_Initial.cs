using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.FellingLicenceApplications.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "FellingLicenceApplications");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

            migrationBuilder.CreateTable(
                name: "FellingLicenceApplication",
                schema: "FellingLicenceApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    ApplicationReference = table.Column<string>(type: "text", nullable: false),
                    CreatedTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    ProposedFellingStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProposedFellingEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActualFellingStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActualFellingEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Source = table.Column<string>(type: "text", nullable: false),
                    ApproverId = table.Column<Guid>(type: "uuid", nullable: true),
                    WoodlandOwnerId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FellingLicenceApplication", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssigneeHistory",
                schema: "FellingLicenceApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    FellingLicenceApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TimestampAssigned = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimestampUnassigned = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Role = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssigneeHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssigneeHistory_FellingLicenceApplication_FellingLicenceApp~",
                        column: x => x.FellingLicenceApplicationId,
                        principalSchema: "FellingLicenceApplications",
                        principalTable: "FellingLicenceApplication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssigneeHistory_UserAccount_AssignedUserId",
                        column: x => x.AssignedUserId,
                        principalSchema: "Applicants",
                        principalTable: "UserAccount",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Document",
                schema: "FellingLicenceApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    FellingLicenceApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    FileType = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Document", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Document_FellingLicenceApplication_FellingLicenceApplicatio~",
                        column: x => x.FellingLicenceApplicationId,
                        principalSchema: "FellingLicenceApplications",
                        principalTable: "FellingLicenceApplication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LinkedPropertyProfile",
                schema: "FellingLicenceApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    PropertyProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    FellingLicenceApplicationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinkedPropertyProfile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LinkedPropertyProfile_FellingLicenceApplication_FellingLice~",
                        column: x => x.FellingLicenceApplicationId,
                        principalSchema: "FellingLicenceApplications",
                        principalTable: "FellingLicenceApplication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StatusHistory",
                schema: "FellingLicenceApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    FellingLicenceApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StatusHistory_FellingLicenceApplication_FellingLicenceAppli~",
                        column: x => x.FellingLicenceApplicationId,
                        principalSchema: "FellingLicenceApplications",
                        principalTable: "FellingLicenceApplication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProposedFellingDetail",
                schema: "FellingLicenceApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    LinkedPropertyProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyProfileCompartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationType = table.Column<string>(type: "text", nullable: false),
                    AreaToBeFelled = table.Column<double>(type: "double precision", nullable: false),
                    NumberOfTrees = table.Column<int>(type: "integer", nullable: true),
                    TreeMarking = table.Column<string>(type: "text", nullable: true),
                    IsPartOfTreePreservationOrder = table.Column<bool>(type: "boolean", nullable: false),
                    IsWithinConservationArea = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProposedFellingDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProposedFellingDetail_LinkedPropertyProfile_LinkedPropertyP~",
                        column: x => x.LinkedPropertyProfileId,
                        principalSchema: "FellingLicenceApplications",
                        principalTable: "LinkedPropertyProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProposedRestockingDetail",
                schema: "FellingLicenceApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    LinkedPropertyProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyProfileCompartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RestockingProposal = table.Column<string>(type: "text", nullable: false),
                    Area = table.Column<double>(type: "double precision", nullable: true),
                    PercentageOfRestockArea = table.Column<int>(type: "integer", nullable: true),
                    RestockingDensity = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProposedRestockingDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProposedRestockingDetail_LinkedPropertyProfile_LinkedProper~",
                        column: x => x.LinkedPropertyProfileId,
                        principalSchema: "FellingLicenceApplications",
                        principalTable: "LinkedPropertyProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FellingOutcome",
                schema: "FellingLicenceApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    ProposedFellingDetailsId = table.Column<Guid>(type: "uuid", nullable: false),
                    Volume = table.Column<int>(type: "integer", nullable: true),
                    NumberOfTrees = table.Column<int>(type: "integer", nullable: true),
                    TreeMarking = table.Column<string>(type: "text", nullable: true),
                    Species = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FellingOutcome", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FellingOutcome_ProposedFellingDetail_ProposedFellingDetails~",
                        column: x => x.ProposedFellingDetailsId,
                        principalSchema: "FellingLicenceApplications",
                        principalTable: "ProposedFellingDetail",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FellingSpecies",
                schema: "FellingLicenceApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    ProposedFellingDetailsId = table.Column<Guid>(type: "uuid", nullable: false),
                    Species = table.Column<string>(type: "text", nullable: false),
                    Percentage = table.Column<int>(type: "integer", nullable: true),
                    Volume = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FellingSpecies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FellingSpecies_ProposedFellingDetail_ProposedFellingDetails~",
                        column: x => x.ProposedFellingDetailsId,
                        principalSchema: "FellingLicenceApplications",
                        principalTable: "ProposedFellingDetail",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RestockingOutcome",
                schema: "FellingLicenceApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    ProposedRestockingDetailsId = table.Column<Guid>(type: "uuid", nullable: false),
                    Area = table.Column<double>(type: "double precision", nullable: false),
                    NumberOfTrees = table.Column<int>(type: "integer", nullable: true),
                    Species = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestockingOutcome", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestockingOutcome_ProposedRestockingDetail_ProposedRestocki~",
                        column: x => x.ProposedRestockingDetailsId,
                        principalSchema: "FellingLicenceApplications",
                        principalTable: "ProposedRestockingDetail",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RestockingSpecies",
                schema: "FellingLicenceApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    ProposedRestockingDetailsId = table.Column<Guid>(type: "uuid", nullable: false),
                    Species = table.Column<string>(type: "text", nullable: false),
                    Percentage = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestockingSpecies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestockingSpecies_ProposedRestockingDetail_ProposedRestocki~",
                        column: x => x.ProposedRestockingDetailsId,
                        principalSchema: "FellingLicenceApplications",
                        principalTable: "ProposedRestockingDetail",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssigneeHistory_AssignedUserId",
                schema: "FellingLicenceApplications",
                table: "AssigneeHistory",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AssigneeHistory_FellingLicenceApplicationId",
                schema: "FellingLicenceApplications",
                table: "AssigneeHistory",
                column: "FellingLicenceApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Document_FellingLicenceApplicationId",
                schema: "FellingLicenceApplications",
                table: "Document",
                column: "FellingLicenceApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_FellingOutcome_ProposedFellingDetailsId",
                schema: "FellingLicenceApplications",
                table: "FellingOutcome",
                column: "ProposedFellingDetailsId");

            migrationBuilder.CreateIndex(
                name: "IX_FellingSpecies_ProposedFellingDetailsId",
                schema: "FellingLicenceApplications",
                table: "FellingSpecies",
                column: "ProposedFellingDetailsId");

            migrationBuilder.CreateIndex(
                name: "IX_LinkedPropertyProfile_FellingLicenceApplicationId",
                schema: "FellingLicenceApplications",
                table: "LinkedPropertyProfile",
                column: "FellingLicenceApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProposedFellingDetail_LinkedPropertyProfileId",
                schema: "FellingLicenceApplications",
                table: "ProposedFellingDetail",
                column: "LinkedPropertyProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ProposedRestockingDetail_LinkedPropertyProfileId",
                schema: "FellingLicenceApplications",
                table: "ProposedRestockingDetail",
                column: "LinkedPropertyProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_RestockingOutcome_ProposedRestockingDetailsId",
                schema: "FellingLicenceApplications",
                table: "RestockingOutcome",
                column: "ProposedRestockingDetailsId");

            migrationBuilder.CreateIndex(
                name: "IX_RestockingSpecies_ProposedRestockingDetailsId",
                schema: "FellingLicenceApplications",
                table: "RestockingSpecies",
                column: "ProposedRestockingDetailsId");

            migrationBuilder.CreateIndex(
                name: "IX_StatusHistory_FellingLicenceApplicationId",
                schema: "FellingLicenceApplications",
                table: "StatusHistory",
                column: "FellingLicenceApplicationId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssigneeHistory",
                schema: "FellingLicenceApplications");

            migrationBuilder.DropTable(
                name: "Document",
                schema: "FellingLicenceApplications");

            migrationBuilder.DropTable(
                name: "FellingOutcome",
                schema: "FellingLicenceApplications");

            migrationBuilder.DropTable(
                name: "FellingSpecies",
                schema: "FellingLicenceApplications");

            migrationBuilder.DropTable(
                name: "RestockingOutcome",
                schema: "FellingLicenceApplications");

            migrationBuilder.DropTable(
                name: "RestockingSpecies",
                schema: "FellingLicenceApplications");

            migrationBuilder.DropTable(
                name: "StatusHistory",
                schema: "FellingLicenceApplications");

            migrationBuilder.DropTable(
                name: "ProposedFellingDetail",
                schema: "FellingLicenceApplications");

            migrationBuilder.DropTable(
                name: "ProposedRestockingDetail",
                schema: "FellingLicenceApplications");

            migrationBuilder.DropTable(
                name: "LinkedPropertyProfile",
                schema: "FellingLicenceApplications");

            migrationBuilder.DropTable(
                name: "FellingLicenceApplication",
                schema: "FellingLicenceApplications");
        }
    }
}
