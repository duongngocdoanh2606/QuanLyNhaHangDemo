using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyNhaHangDemo.Migrations
{
    public partial class AddZoneForTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Zone",
                table: "Table");

            migrationBuilder.AddColumn<int>(
                name: "ZoneId",
                table: "Table",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Zones",
                columns: table => new
                {
                    ZoneId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ZoneName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ZoneDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zones", x => x.ZoneId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Table_ZoneId",
                table: "Table",
                column: "ZoneId");

            migrationBuilder.AddForeignKey(
                name: "FK_Table_Zones_ZoneId",
                table: "Table",
                column: "ZoneId",
                principalTable: "Zones",
                principalColumn: "ZoneId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Table_Zones_ZoneId",
                table: "Table");

            migrationBuilder.DropTable(
                name: "Zones");

            migrationBuilder.DropIndex(
                name: "IX_Table_ZoneId",
                table: "Table");

            migrationBuilder.DropColumn(
                name: "ZoneId",
                table: "Table");

            migrationBuilder.AddColumn<string>(
                name: "Zone",
                table: "Table",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
