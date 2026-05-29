using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyNhaHangDemo.Migrations
{
    public partial class AddZoneStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Zones");

            migrationBuilder.AddColumn<int>(
                name: "ZoneStatus",
                table: "Zones",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ZoneStatus",
                table: "Zones");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Zones",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
