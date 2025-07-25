﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.PropertyProfiles.Migrations
{
    public partial class SortPropertyMigrations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PropertyProfile_WoodlandOwner_WoodlandOwnerId",
                schema: "PropertyProfiles",
                table: "PropertyProfile");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_PropertyProfile_WoodlandOwner_WoodlandOwnerId",
                schema: "PropertyProfiles",
                table: "PropertyProfile",
                column: "WoodlandOwnerId",
                principalSchema: "Applicants",
                principalTable: "WoodlandOwner",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
