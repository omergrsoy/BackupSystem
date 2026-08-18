using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackupSystem.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddSchedulingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DailyBackupTime",
                table: "Machines",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastScheduledBackupDate",
                table: "Machines",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyBackupTime",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "LastScheduledBackupDate",
                table: "Machines");
        }
    }
}
