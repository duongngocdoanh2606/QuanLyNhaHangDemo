using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyNhaHangDemo.Migrations
{
    public partial class AddModifierToProduct : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Modifiers_Materials_MaterialId",
                table: "Modifiers");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetailModifierModel_Modifiers_ModifierId",
                table: "OrderDetailModifierModel");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetailModifierModel_OrderDetails_OrderDetailId",
                table: "OrderDetailModifierModel");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_ProductVariants_VariantId",
                table: "OrderDetails");

            migrationBuilder.DropTable(
                name: "ProductVariantMaterials");

            migrationBuilder.DropTable(
                name: "Ratings");

            migrationBuilder.DropTable(
                name: "Shippings");

            migrationBuilder.DropTable(
                name: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_OrderDetails_VariantId",
                table: "OrderDetails");

            migrationBuilder.DropIndex(
                name: "IX_Modifiers_MaterialId",
                table: "Modifiers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderDetailModifierModel",
                table: "OrderDetailModifierModel");

            migrationBuilder.DropColumn(
                name: "MaterialId",
                table: "Modifiers");

            migrationBuilder.RenameTable(
                name: "OrderDetailModifierModel",
                newName: "OrderDetailModifiers");

            migrationBuilder.RenameIndex(
                name: "IX_OrderDetailModifierModel_OrderDetailId",
                table: "OrderDetailModifiers",
                newName: "IX_OrderDetailModifiers_OrderDetailId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderDetailModifierModel_ModifierId",
                table: "OrderDetailModifiers",
                newName: "IX_OrderDetailModifiers_ModifierId");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Multiplier",
                table: "Modifiers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Modifiers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderDetailModifiers",
                table: "OrderDetailModifiers",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "ModifierMaterials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModifierId = table.Column<int>(type: "int", nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    QuantityRequired = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModifierMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModifierMaterials_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ModifierMaterials_Modifiers_ModifierId",
                        column: x => x.ModifierId,
                        principalTable: "Modifiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "productMaterials",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    QuantityRequired = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productMaterials", x => new { x.ProductId, x.MaterialId });
                    table.ForeignKey(
                        name: "FK_productMaterials_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_productMaterials_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModifierMaterials_MaterialId",
                table: "ModifierMaterials",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ModifierMaterials_ModifierId",
                table: "ModifierMaterials",
                column: "ModifierId");

            migrationBuilder.CreateIndex(
                name: "IX_productMaterials_MaterialId",
                table: "productMaterials",
                column: "MaterialId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetailModifiers_Modifiers_ModifierId",
                table: "OrderDetailModifiers",
                column: "ModifierId",
                principalTable: "Modifiers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetailModifiers_OrderDetails_OrderDetailId",
                table: "OrderDetailModifiers",
                column: "OrderDetailId",
                principalTable: "OrderDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetailModifiers_Modifiers_ModifierId",
                table: "OrderDetailModifiers");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetailModifiers_OrderDetails_OrderDetailId",
                table: "OrderDetailModifiers");

            migrationBuilder.DropTable(
                name: "ModifierMaterials");

            migrationBuilder.DropTable(
                name: "productMaterials");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderDetailModifiers",
                table: "OrderDetailModifiers");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Multiplier",
                table: "Modifiers");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Modifiers");

            migrationBuilder.RenameTable(
                name: "OrderDetailModifiers",
                newName: "OrderDetailModifierModel");

            migrationBuilder.RenameIndex(
                name: "IX_OrderDetailModifiers_OrderDetailId",
                table: "OrderDetailModifierModel",
                newName: "IX_OrderDetailModifierModel_OrderDetailId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderDetailModifiers_ModifierId",
                table: "OrderDetailModifierModel",
                newName: "IX_OrderDetailModifierModel_ModifierId");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Orders",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "MaterialId",
                table: "Modifiers",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderDetailModifierModel",
                table: "OrderDetailModifierModel",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "ProductVariants",
                columns: table => new
                {
                    VariantId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VariantName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariants", x => x.VariantId);
                    table.ForeignKey(
                        name: "FK_ProductVariants_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Ratings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Stars = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ratings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ratings_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Shippings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    District = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Ward = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shippings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariantMaterials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    ProductVariantId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariantMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariantMaterials_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductVariantMaterials_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "VariantId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_VariantId",
                table: "OrderDetails",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_Modifiers_MaterialId",
                table: "Modifiers",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantMaterials_MaterialId",
                table: "ProductVariantMaterials",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantMaterials_ProductVariantId",
                table: "ProductVariantMaterials",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId",
                table: "ProductVariants",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_ProductId",
                table: "Ratings",
                column: "ProductId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Modifiers_Materials_MaterialId",
                table: "Modifiers",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetailModifierModel_Modifiers_ModifierId",
                table: "OrderDetailModifierModel",
                column: "ModifierId",
                principalTable: "Modifiers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetailModifierModel_OrderDetails_OrderDetailId",
                table: "OrderDetailModifierModel",
                column: "OrderDetailId",
                principalTable: "OrderDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_ProductVariants_VariantId",
                table: "OrderDetails",
                column: "VariantId",
                principalTable: "ProductVariants",
                principalColumn: "VariantId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
