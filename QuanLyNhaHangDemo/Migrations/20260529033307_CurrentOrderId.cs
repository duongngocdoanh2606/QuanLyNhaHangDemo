using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyNhaHangDemo.Migrations
{
    public partial class CurrentOrderId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Table_TableId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_TableId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TableId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DefaultPrepTime",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Categories");

            migrationBuilder.AddColumn<int>(
                name: "CurrentOrderId",
                table: "Table",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Table_CurrentOrderId",
                table: "Table",
                column: "CurrentOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Table_Orders_CurrentOrderId",
                table: "Table",
                column: "CurrentOrderId",
                principalTable: "Orders",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Table_Orders_CurrentOrderId",
                table: "Table");

            migrationBuilder.DropIndex(
                name: "IX_Table_CurrentOrderId",
                table: "Table");

            migrationBuilder.DropColumn(
                name: "CurrentOrderId",
                table: "Table");

            migrationBuilder.AddColumn<int>(
                name: "TableId",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DefaultPrepTime",
                table: "Categories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "Categories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TableId",
                table: "Orders",
                column: "TableId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Table_TableId",
                table: "Orders",
                column: "TableId",
                principalTable: "Table",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
