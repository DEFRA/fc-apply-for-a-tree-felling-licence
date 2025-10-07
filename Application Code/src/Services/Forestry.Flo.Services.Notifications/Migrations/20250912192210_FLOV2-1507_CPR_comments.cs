using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.Notifications.Migrations
{
    [ExcludeFromCodeCoverage]
    /// <inheritdoc />
    public partial class FLOV21507_CPR_comments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ExternalId",
                schema: "Notifications",
                table: "NotificationHistory",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Response",
                schema: "Notifications",
                table: "NotificationHistory",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "Notifications",
                table: "NotificationHistory",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationHistory_ExternalId",
                schema: "Notifications",
                table: "NotificationHistory",
                column: "ExternalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotificationHistory_ExternalId",
                schema: "Notifications",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                schema: "Notifications",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "Response",
                schema: "Notifications",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "Notifications",
                table: "NotificationHistory");
        }
    }
}
