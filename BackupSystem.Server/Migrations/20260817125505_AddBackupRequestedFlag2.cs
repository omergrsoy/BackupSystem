using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackupSystem.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddBackupRequestedFlag2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastHeartBeat",
                table: "Machines",
                newName: "LastHeartbeat");

            migrationBuilder.AddColumn<bool>(
                name: "IsBackupRequested",
                table: "Machines",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBackupRequested",
                table: "Machines");

            migrationBuilder.RenameColumn(
                name: "LastHeartbeat",
                table: "Machines",
                newName: "LastHeartBeat");
        }
    }
}
