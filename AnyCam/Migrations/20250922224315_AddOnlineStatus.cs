using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnyCam.Migrations
{
    /// <inheritdoc />
    public partial class AddOnlineStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOnline",
                table: "Cameras");

            migrationBuilder.DropColumn(
                name: "LastChecked",
                table: "Cameras");
        }
    }
}
