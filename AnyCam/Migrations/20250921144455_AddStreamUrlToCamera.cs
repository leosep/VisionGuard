using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnyCam.Migrations
{
    /// <inheritdoc />
    public partial class AddStreamUrlToCamera : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StreamUrl",
                table: "Cameras",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StreamUrl",
                table: "Cameras");
        }
    }
}
