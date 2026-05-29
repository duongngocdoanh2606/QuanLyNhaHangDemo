using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyNhaHangDemo.Migrations
{
    public partial class AddHeThong : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductModifierMappings_ModifierGroups_ModifierGroupId",
                table: "ProductModifierMappings");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariant_Products_ProductId",
                table: "ProductVariant");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariantMaterials_Materials_MaterialId",
                table: "ProductVariantMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariantMaterials_ProductVariant_ProductVariantId",
                table: "ProductVariantMaterials");

            migrationBuilder.DropTable(
                name: "ProductMaterials");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductModifierMappings",
                table: "ProductModifierMappings");

            migrationBuilder.DropIndex(
                name: "IX_ProductModifierMappings_ProductId",
                table: "ProductModifierMappings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductVariant",
                table: "ProductVariant");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ProductModifierMappings");

            migrationBuilder.RenameTable(
                name: "ProductVariant",
                newName: "ProductVariants");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "Products",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ProductVariants",
                newName: "VariantId");

            migrationBuilder.RenameIndex(
                name: "IX_ProductVariant_ProductId",
                table: "ProductVariants",
                newName: "IX_ProductVariants_ProductId");

            migrationBuilder.AddColumn<int>(
                name: "VariantId",
                table: "OrderDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaterialId",
                table: "Modifiers",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductModifierMappings",
                table: "ProductModifierMappings",
                columns: new[] { "ProductId", "ModifierGroupId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductVariants",
                table: "ProductVariants",
                column: "VariantId");

            migrationBuilder.CreateTable(
                name: "OrderDetailModifierModel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderDetailId = table.Column<int>(type: "int", nullable: false),
                    ModifierId = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderDetailModifierModel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderDetailModifierModel_Modifiers_ModifierId",
                        column: x => x.ModifierId,
                        principalTable: "Modifiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderDetailModifierModel_OrderDetails_OrderDetailId",
                        column: x => x.OrderDetailId,
                        principalTable: "OrderDetails",
                        principalColumn: "Id",
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
                name: "IX_OrderDetailModifierModel_ModifierId",
                table: "OrderDetailModifierModel",
                column: "ModifierId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetailModifierModel_OrderDetailId",
                table: "OrderDetailModifierModel",
                column: "OrderDetailId");

            migrationBuilder.AddForeignKey(
                name: "FK_Modifiers_Materials_MaterialId",
                table: "Modifiers",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_ProductVariants_VariantId",
                table: "OrderDetails",
                column: "VariantId",
                principalTable: "ProductVariants",
                principalColumn: "VariantId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductModifierMappings_ModifierGroups_ModifierGroupId",
                table: "ProductModifierMappings",
                column: "ModifierGroupId",
                principalTable: "ModifierGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariantMaterials_Materials_MaterialId",
                table: "ProductVariantMaterials",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariantMaterials_ProductVariants_ProductVariantId",
                table: "ProductVariantMaterials",
                column: "ProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "VariantId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariants_Products_ProductId",
                table: "ProductVariants",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Modifiers_Materials_MaterialId",
                table: "Modifiers");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_ProductVariants_VariantId",
                table: "OrderDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductModifierMappings_ModifierGroups_ModifierGroupId",
                table: "ProductModifierMappings");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariantMaterials_Materials_MaterialId",
                table: "ProductVariantMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariantMaterials_ProductVariants_ProductVariantId",
                table: "ProductVariantMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariants_Products_ProductId",
                table: "ProductVariants");

            migrationBuilder.DropTable(
                name: "OrderDetailModifierModel");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductModifierMappings",
                table: "ProductModifierMappings");

            migrationBuilder.DropIndex(
                name: "IX_OrderDetails_VariantId",
                table: "OrderDetails");

            migrationBuilder.DropIndex(
                name: "IX_Modifiers_MaterialId",
                table: "Modifiers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductVariants",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "VariantId",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "MaterialId",
                table: "Modifiers");

            migrationBuilder.RenameTable(
                name: "ProductVariants",
                newName: "ProductVariant");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Products",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "VariantId",
                table: "ProductVariant",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_ProductVariants_ProductId",
                table: "ProductVariant",
                newName: "IX_ProductVariant_ProductId");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "ProductModifierMappings",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductModifierMappings",
                table: "ProductModifierMappings",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductVariant",
                table: "ProductVariant",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "ProductMaterials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QuantityPerProduct = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductMaterials_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductMaterials_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductModifierMappings_ProductId",
                table: "ProductModifierMappings",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMaterials_MaterialId",
                table: "ProductMaterials",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMaterials_ProductId",
                table: "ProductMaterials",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductModifierMappings_ModifierGroups_ModifierGroupId",
                table: "ProductModifierMappings",
                column: "ModifierGroupId",
                principalTable: "ModifierGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariant_Products_ProductId",
                table: "ProductVariant",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariantMaterials_Materials_MaterialId",
                table: "ProductVariantMaterials",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariantMaterials_ProductVariant_ProductVariantId",
                table: "ProductVariantMaterials",
                column: "ProductVariantId",
                principalTable: "ProductVariant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
