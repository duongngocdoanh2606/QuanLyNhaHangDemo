using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyNhaHangDemo.Migrations
{
    public partial class AddZoneAndPayment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Coupon_CouponId",
                table: "Orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Coupon",
                table: "Coupon");

            migrationBuilder.DropColumn(
                name: "ShippingCost",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CreateAt",
                table: "Coupon");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "Coupon");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Coupon");

            migrationBuilder.DropColumn(
                name: "MaxDiscountAmount",
                table: "Coupon");

            migrationBuilder.DropColumn(
                name: "MinOrderAmount",
                table: "Coupon");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Coupon");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Coupon");

            migrationBuilder.DropColumn(
                name: "UsageLimit",
                table: "Coupon");

            migrationBuilder.DropColumn(
                name: "UsedCount",
                table: "Coupon");

            migrationBuilder.RenameTable(
                name: "Coupon",
                newName: "CouponModel");

            migrationBuilder.RenameColumn(
                name: "DiscountValue",
                table: "CouponModel",
                newName: "DiscountAmount");

            migrationBuilder.AddColumn<string>(
                name: "Zone",
                table: "Table",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Method",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PayStatus",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_CouponModel",
                table: "CouponModel",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_CouponModel_CouponId",
                table: "Orders",
                column: "CouponId",
                principalTable: "CouponModel",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_CouponModel_CouponId",
                table: "Orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CouponModel",
                table: "CouponModel");

            migrationBuilder.DropColumn(
                name: "Zone",
                table: "Table");

            migrationBuilder.DropColumn(
                name: "Method",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PayStatus",
                table: "Orders");

            migrationBuilder.RenameTable(
                name: "CouponModel",
                newName: "Coupon");

            migrationBuilder.RenameColumn(
                name: "DiscountAmount",
                table: "Coupon",
                newName: "DiscountValue");

            migrationBuilder.AddColumn<decimal>(
                name: "ShippingCost",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateAt",
                table: "Coupon",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "DiscountType",
                table: "Coupon",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Coupon",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "MaxDiscountAmount",
                table: "Coupon",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinOrderAmount",
                table: "Coupon",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Coupon",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Coupon",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "UsageLimit",
                table: "Coupon",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UsedCount",
                table: "Coupon",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Coupon",
                table: "Coupon",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Coupon_CouponId",
                table: "Orders",
                column: "CouponId",
                principalTable: "Coupon",
                principalColumn: "Id");
        }
    }
}
