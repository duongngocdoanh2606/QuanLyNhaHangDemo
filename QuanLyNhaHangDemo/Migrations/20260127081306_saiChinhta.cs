using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyNhaHangDemo.Migrations
{
    public partial class saiChinhta : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Kitchen_KitchenId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "KetchenId",
                table: "Categories");

            migrationBuilder.AlterColumn<int>(
                name: "KitchenId",
                table: "Categories",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Kitchen_KitchenId",
                table: "Categories",
                column: "KitchenId",
                principalTable: "Kitchen",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Kitchen_KitchenId",
                table: "Categories");

            migrationBuilder.AlterColumn<int>(
                name: "KitchenId",
                table: "Categories",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "KetchenId",
                table: "Categories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Kitchen_KitchenId",
                table: "Categories",
                column: "KitchenId",
                principalTable: "Kitchen",
                principalColumn: "Id");
        }
    }
}
