using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyNhaHangDemo.Migrations
{
    public partial class AddWidthHeightToTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "Height",
                table: "Table",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Width",
                table: "Table",
                type: "real",
                nullable: false,
                defaultValue: 0f);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Height",
                table: "Table");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "Table");
        }
    }
}
