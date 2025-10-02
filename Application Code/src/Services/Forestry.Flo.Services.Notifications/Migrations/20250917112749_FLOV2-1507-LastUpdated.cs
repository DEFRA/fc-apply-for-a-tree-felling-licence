using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.Notifications.Migrations
{
    [ExcludeFromCodeCoverage]
    /// <inheritdoc />
    public partial class FLOV21507LastUpdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LastUpdatedById",
                schema: "Notifications",
                table: "NotificationHistory",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedDate",
                schema: "Notifications",
                table: "NotificationHistory",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastUpdatedById",
                schema: "Notifications",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "LastUpdatedDate",
                schema: "Notifications",
                table: "NotificationHistory");
        }
    }
}
