using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Models;
using System.Data.Common;

namespace QuanLyNhaHangDemo.Repository
{
    public class DataContext : IdentityDbContext<AppUserModel>
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {

        }

        public DbSet<ProductModel> Products { get; set; }
        public DbSet<CategoryModel> Categories { get; set; }
        public DbSet<OrderModel> Orders { get; set; }
        public DbSet<OrderDetailsModel> OrderDetails { get; set; }
        public DbSet<ProductQuantityModel> ProductQuantities { get; set; }
        public DbSet<StatisticalModel> Statisticals { get; set; }
        public DbSet<MaterialModel> Materials { get; set; }
        public DbSet<InventoryTransactionModel> InventoryTransactions { get; set; }
        public DbSet<UserModel> User { get; set; }
        public DbSet<TableModel> Table { get; set; }
        public DbSet<ReservationModel> Reservations { get; set; }
        public DbSet<SupplierModel> Suppliers { get; set; }
        public DbSet<SupplierCategoryModel> SupplierCategories { get; set; }
        public DbSet<CouponModel> Coupon { get; set; }
        public DbSet<KitchenModel> Kitchen { get; set; }

        // --- Danh mục nhóm lựa chọn & lựa chọn ---
        public DbSet<ModifierGroupModel> ModifierGroups { get; set; }
        public DbSet<ModifierModel> Modifiers { get; set; }
        public DbSet<ProModifierGroupModel> ProductModifierMappings { get; set; }

        // --- 🚀 BỔ SUNG: Bảng định mức nguyên liệu hao hụt của Modifier ---
        public DbSet<ModifierMaterialModel> ModifierMaterials { get; set; }

        // --- Chi tiết đơn hàng khi khách chọn món kèm Modifier ---
        public DbSet<OrderDetailModifierModel> OrderDetailModifiers { get; set; }
        public DbSet<ProductMaterialsModel> productMaterials { get; set; }
        public DbSet<AdminNotificationModel> AdminNotifications { get; set; }
        public DbSet<CouponModel> Coupons { get; set; }
        public DbSet<ZoneModel> Zones { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- 1. Keys ---

            modelBuilder.Entity<ProModifierGroupModel>()
                .HasKey(pmg => new { pmg.ProductId, pmg.ModifierGroupId });

            // --- 2. Query Filter (Soft delete) ---

            modelBuilder.Entity<ProductModel>()
                .HasQueryFilter(p => p.Status == 1);

            modelBuilder.Entity<ModifierGroupModel>()
                .HasQueryFilter(mg => mg.Status == 1);

            modelBuilder.Entity<ModifierModel>()
                .HasQueryFilter(m => m.Status == 1);

            // --- 3. Relationships ---

            // Material → Supplier
            modelBuilder.Entity<MaterialModel>()
                .HasOne(m => m.Supplier)
                .WithMany(s => s.Materials)
                .HasForeignKey(m => m.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            // Supplier → SupplierCategory
            modelBuilder.Entity<SupplierModel>()
                .HasOne(s => s.SupplierCategory)
                .WithMany(sc => sc.Suppliers)
                .HasForeignKey(s => s.SupplierCategoryId)
                .OnDelete(DeleteBehavior.Restrict);



            // Product → ProModifierGroup
            modelBuilder.Entity<ProModifierGroupModel>()
                .HasOne(pmg => pmg.Product)
                .WithMany(p => p.ProductModifierGroups)
                .HasForeignKey(pmg => pmg.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // ModifierGroup → ProModifierGroup
            modelBuilder.Entity<ProModifierGroupModel>()
                .HasOne(pmg => pmg.ModifierGroup)
                .WithMany(mg => mg.ProductModifierMappings)
                .HasForeignKey(pmg => pmg.ModifierGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product → Category
            modelBuilder.Entity<ProductModel>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // OrderDetailModifier
            modelBuilder.Entity<OrderDetailModifierModel>(entity =>
            {
                entity.HasKey(odm => odm.Id);

                // 1 Chi tiết đơn hàng có nhiều Modifier lựa chọn
                entity.HasOne(odm => odm.OrderDetail)
                      .WithMany(od => od.OrderDetailModifiers)
                      .HasForeignKey(odm => odm.OrderDetailId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Liên kết sang bảng danh mục Modifier gốc
                entity.HasOne(odm => odm.Modifier)
                      .WithMany()
                      .HasForeignKey(odm => odm.ModifierId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // --- 🚀 BỔ SUNG: Cấu hình cho bảng mới ModifierMaterialModel ---
            modelBuilder.Entity<ModifierMaterialModel>(entity =>
            {
                entity.HasKey(mm => mm.Id);

                // Một Modifier (ví dụ: Thêm Tôm) có thể có nhiều nguyên liệu định mức
                entity.HasOne(mm => mm.Modifier)
                      .WithMany(m => m.ModifierMaterials)
                      .HasForeignKey(mm => mm.ModifierId)
                      .OnDelete(DeleteBehavior.Cascade); // Xóa lựa chọn thì tự động xóa định mức nguyên liệu đi kèm

                // Liên kết tới bảng Nguyên liệu gốc
                entity.HasOne(mm => mm.Material)
                      .WithMany() // Nếu bên MaterialModel không khai báo Collection, cứ để trống WithMany()
                      .HasForeignKey(mm => mm.MaterialId)
                      .OnDelete(DeleteBehavior.Restrict); // Không cho phép xóa nguyên liệu nếu đang được gắn vào một Modifier
            });
            modelBuilder.Entity<OrderModel>()
                .Property(o => o.Status)
                .HasConversion(
                    v => v.ToString(),                             // Khi lưu xuống DB: Chuyển Enum thành String
                    v => (OrderModel.OrderStatus)Enum.Parse(typeof(OrderModel.OrderStatus), v) // Khi đọc từ DB lên: Ép String ngược lại thành Enum
                );
            modelBuilder.Entity<ProductMaterialsModel>()
                .HasKey(pm => new { pm.ProductId, pm.MaterialId });
        }
    }
}