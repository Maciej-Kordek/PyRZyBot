using Microsoft.EntityFrameworkCore.Migrations;

namespace PyRZyBot_2._0.Migrations
{
    public partial class _050521 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ToRank",
                table: "ChatUsers_S",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ToRank",
                table: "ChatUsers_S");
        }
    }
}
