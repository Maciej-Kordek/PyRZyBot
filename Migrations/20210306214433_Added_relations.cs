using Microsoft.EntityFrameworkCore.Migrations;

namespace PyRZyBot_2._0.Migrations
{
    public partial class Added_relations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "TwitchId",
                table: "ChatUsers_S",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TwitchId",
                table: "ChatUsers",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ChatUsers_S_TwitchId",
                table: "ChatUsers_S",
                column: "TwitchId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatUsers_TwitchId",
                table: "ChatUsers",
                column: "TwitchId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatUsers_ChatUsers_S_TwitchId",
                table: "ChatUsers",
                column: "TwitchId",
                principalTable: "ChatUsers_S",
                principalColumn: "TwitchId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatUsers_ChatUsers_S_TwitchId",
                table: "ChatUsers");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ChatUsers_S_TwitchId",
                table: "ChatUsers_S");

            migrationBuilder.DropIndex(
                name: "IX_ChatUsers_TwitchId",
                table: "ChatUsers");

            migrationBuilder.AlterColumn<string>(
                name: "TwitchId",
                table: "ChatUsers_S",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "TwitchId",
                table: "ChatUsers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
