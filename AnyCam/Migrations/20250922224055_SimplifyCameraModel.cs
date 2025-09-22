using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnyCam.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyCameraModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "Cameras");

            migrationBuilder.DropColumn(
                name: "IsOnline",
                table: "Cameras");

            migrationBuilder.DropColumn(
                name: "LastChecked",
                table: "Cameras");

            migrationBuilder.DropColumn(
                name: "Password",
                table: "Cameras");

            migrationBuilder.DropColumn(
                name: "Port",
                table: "Cameras");

            migrationBuilder.DropColumn(
                name: "Protocol",
                table: "Cameras");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "Cameras");

            migrationBuilder.RenameColumn(
                name: "StreamType",
                table: "Cameras",
                newName: "Location");

            migrationBuilder.AlterColumn<string>(
                name: "StreamUrl",
                table: "Cameras",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Location",
                table: "Cameras",
                newName: "StreamType");

            migrationBuilder.AlterColumn<string>(
                name: "StreamUrl",
                table: "Cameras",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "Cameras",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsOnline",
                table: "Cameras",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastChecked",
                table: "Cameras",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "Cameras",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Port",
                table: "Cameras",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Protocol",
                table: "Cameras",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Cameras",
                type: "TEXT",
                nullable: true);
        }
    }
}
