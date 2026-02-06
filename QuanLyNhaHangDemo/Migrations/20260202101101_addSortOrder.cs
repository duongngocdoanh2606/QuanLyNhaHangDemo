using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyNhaHangDemo.Migrations
{
    public partial class addSortOrder : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_tableModels_TableId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_tableModels_TableId",
                table: "Reservations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_userModels",
                table: "userModels");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tableModels",
                table: "tableModels");

            migrationBuilder.RenameTable(
                name: "userModels",
                newName: "User");

            migrationBuilder.RenameTable(
                name: "tableModels",
                newName: "Table");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateDate",
                table: "OrderDetails",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Kitchen",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Kitchen",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_User",
                table: "User",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Table",
                table: "Table",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Table_TableId",
                table: "Orders",
                column: "TableId",
                principalTable: "Table",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Table_TableId",
                table: "Reservations",
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

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Table_TableId",
                table: "Reservations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_User",
                table: "User");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Table",
                table: "Table");

            migrationBuilder.DropColumn(
                name: "CreateDate",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Kitchen");

            migrationBuilder.RenameTable(
                name: "User",
                newName: "userModels");

            migrationBuilder.RenameTable(
                name: "Table",
                newName: "tableModels");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Kitchen",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_userModels",
                table: "userModels",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tableModels",
                table: "tableModels",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_tableModels_TableId",
                table: "Orders",
                column: "TableId",
                principalTable: "tableModels",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_tableModels_TableId",
                table: "Reservations",
                column: "TableId",
                principalTable: "tableModels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
