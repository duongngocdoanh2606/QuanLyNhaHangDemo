using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyNhaHangDemo.Migrations
{
    public partial class ChangeType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Modifiers");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "ModifierGroups",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "ModifierGroups");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Modifiers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
