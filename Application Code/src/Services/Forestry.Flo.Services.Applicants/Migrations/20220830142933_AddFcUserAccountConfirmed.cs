﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.Applicants.Migrations
{
    public partial class AddFcUserAccountConfirmed : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FcUserAccountConfirmed",
                schema: "Applicants",
                table: "UserAccount",
                type: "boolean",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FcUserAccountConfirmed",
                schema: "Applicants",
                table: "UserAccount");
        }
    }
}
