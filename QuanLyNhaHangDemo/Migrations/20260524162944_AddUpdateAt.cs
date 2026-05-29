using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyNhaHangDemo.Migrations
{
    public partial class AddUpdateAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Table_TableId",
                table: "Orders");

            migrationBuilder.AlterColumn<int>(
                name: "TableId",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "OrderDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Table_TableId",
                table: "Orders",
                column: "TableId",
                principalTable: "Table",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Table_TableId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "OrderDetails");

            migrationBuilder.AlterColumn<int>(
                name: "TableId",
                table: "Orders",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Table_TableId",
                table: "Orders",
                column: "TableId",
                principalTable: "Table",
                principalColumn: "Id");
        }
    }
}
