using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnyCam.Migrations
{
    /// <inheritdoc />
    public partial class AddCameraToAiEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AiEvents_VideoClips_VideoClipId",
                table: "AiEvents");

            migrationBuilder.AlterColumn<string>(
                name: "StreamUrl",
                table: "Cameras",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "VideoClipId",
                table: "AiEvents",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "DetectedObjects",
                table: "AiEvents",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "AlertType",
                table: "AiEvents",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<int>(
                name: "CameraId",
                table: "AiEvents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Confidence",
                table: "AiEvents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiEvents_CameraId",
                table: "AiEvents",
                column: "CameraId");

            migrationBuilder.AddForeignKey(
                name: "FK_AiEvents_Cameras_CameraId",
                table: "AiEvents",
                column: "CameraId",
                principalTable: "Cameras",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AiEvents_VideoClips_VideoClipId",
                table: "AiEvents",
                column: "VideoClipId",
                principalTable: "VideoClips",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AiEvents_Cameras_CameraId",
                table: "AiEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_AiEvents_VideoClips_VideoClipId",
                table: "AiEvents");

            migrationBuilder.DropIndex(
                name: "IX_AiEvents_CameraId",
                table: "AiEvents");

            migrationBuilder.DropColumn(
                name: "CameraId",
                table: "AiEvents");

            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "AiEvents");

            migrationBuilder.AlterColumn<string>(
                name: "StreamUrl",
                table: "Cameras",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "VideoClipId",
                table: "AiEvents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DetectedObjects",
                table: "AiEvents",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AlertType",
                table: "AiEvents",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AiEvents_VideoClips_VideoClipId",
                table: "AiEvents",
                column: "VideoClipId",
                principalTable: "VideoClips",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
