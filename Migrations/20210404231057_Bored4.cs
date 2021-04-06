using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PyRZyBot_2._0.Migrations
{
    public partial class Bored4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastDuel",
                table: "ChatUsers_S",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "Deletable",
                table: "ChannelCommands",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "GameSpecific",
                table: "ChannelCommands",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastDuel",
                table: "ChatUsers_S");

            migrationBuilder.DropColumn(
                name: "Deletable",
                table: "ChannelCommands");

            migrationBuilder.DropColumn(
                name: "GameSpecific",
                table: "ChannelCommands");
        }
    }
}
