using Microsoft.EntityFrameworkCore.Migrations;

namespace PyRZyBot_2._0.Migrations
{
    public partial class Bored_Of_Naming_Migrations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChannelCommands_Aliases_CommandName",
                table: "ChannelCommands");

            migrationBuilder.DropIndex(
                name: "IX_ChannelCommands_CommandName",
                table: "ChannelCommands");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Aliases_CommandName",
                table: "Aliases");

            migrationBuilder.DropColumn(
                name: "CommandName",
                table: "Aliases");

            migrationBuilder.AlterColumn<string>(
                name: "CommandName",
                table: "ChannelCommands",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Channel",
                table: "Aliases",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CommandId",
                table: "Aliases",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Aliases_CommandId",
                table: "Aliases",
                column: "CommandId");

            migrationBuilder.AddForeignKey(
                name: "FK_Aliases_ChannelCommands_CommandId",
                table: "Aliases",
                column: "CommandId",
                principalTable: "ChannelCommands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Aliases_ChannelCommands_CommandId",
                table: "Aliases");

            migrationBuilder.DropIndex(
                name: "IX_Aliases_CommandId",
                table: "Aliases");

            migrationBuilder.DropColumn(
                name: "Channel",
                table: "Aliases");

            migrationBuilder.DropColumn(
                name: "CommandId",
                table: "Aliases");

            migrationBuilder.AlterColumn<string>(
                name: "CommandName",
                table: "ChannelCommands",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommandName",
                table: "Aliases",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Aliases_CommandName",
                table: "Aliases",
                column: "CommandName");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelCommands_CommandName",
                table: "ChannelCommands",
                column: "CommandName");

            migrationBuilder.AddForeignKey(
                name: "FK_ChannelCommands_Aliases_CommandName",
                table: "ChannelCommands",
                column: "CommandName",
                principalTable: "Aliases",
                principalColumn: "CommandName",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
