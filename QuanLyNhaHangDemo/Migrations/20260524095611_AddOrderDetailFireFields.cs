using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyNhaHangDemo.Migrations
{
    public partial class AddOrderDetailFireFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FireCount",
                table: "OrderDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FiredAt",
                table: "OrderDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFired",
                table: "OrderDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                "UPDATE OrderDetails SET IsFired = 1, FiredAt = COALESCE(FiredAt, CreateDate) WHERE IsFired = 0");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FireCount",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "FiredAt",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "IsFired",
                table: "OrderDetails");
        }
    }
}
