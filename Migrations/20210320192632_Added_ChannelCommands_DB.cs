using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PyRZyBot_2._0.Migrations
{
    public partial class Added_ChannelCommands_DB : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "TimeoutTill",
                table: "ChatUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "Aliases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommandName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Alias = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aliases", x => x.Id);
                    table.UniqueConstraint("AK_Aliases_CommandName", x => x.CommandName);
                });

            migrationBuilder.CreateTable(
                name: "ChannelCommands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommandName = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Response = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParentCommand = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Channel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cooldown = table.Column<int>(type: "int", nullable: false),
                    LastUsed = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TimesUsed = table.Column<int>(type: "int", nullable: false),
                    Timer = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsComplex = table.Column<bool>(type: "bit", nullable: false),
                    ToDisplay = table.Column<bool>(type: "bit", nullable: false),
                    AccessLevel = table.Column<int>(type: "int", nullable: false),
                    EditLevel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelCommands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChannelCommands_Aliases_CommandName",
                        column: x => x.CommandName,
                        principalTable: "Aliases",
                        principalColumn: "CommandName",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelCommands_CommandName",
                table: "ChannelCommands",
                column: "CommandName");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChannelCommands");

            migrationBuilder.DropTable(
                name: "Aliases");

            migrationBuilder.DropColumn(
                name: "TimeoutTill",
                table: "ChatUsers");
        }
    }
}
