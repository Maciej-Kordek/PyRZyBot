using Microsoft.EntityFrameworkCore.Migrations;

namespace PyRZyBot_2._0.Migrations
{
    public partial class Changed_QuestionSpam_Collumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "QuestionSpam",
                table: "ChatUsers",
                newName: "RequestSpam");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RequestSpam",
                table: "ChatUsers",
                newName: "QuestionSpam");
        }
    }
}
