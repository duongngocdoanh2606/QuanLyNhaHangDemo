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

        public DbSet<BrandModel> Brands { get; set; }
        public DbSet<ProductModel> Products { get; set; }
        public DbSet<CategoryModel> Categories { get; set; }
        public DbSet<OrderModel> Orders { get; set; }
        public DbSet<OrderDetails> OrderDetails { get; set; }
        public DbSet<SliderModel> Sliders { get; set; }
        public DbSet<RatingModel> Ratings { get; set; }

        public DbSet<ProductQuantityModel>  ProductQuantities { get; set; }
        public DbSet<ShippingModel> Shippings { get; set; }

        public DbSet<StatisticalModel> Statisticals { get; set; }
        public DbSet<MaterialModel> Materials { get; set; }
        public DbSet<InventoryTransactionModel> InventoryTransactions { get; set; }
        public DbSet<ProductMaterialModel> ProductMaterials { get; set; }

        public DbSet<UserModel> User { get; set; }

        public DbSet<TableModel> Table { get; set; }
        public DbSet<ReservationModel> Reservations { get; set; }

        public DbSet<SupplierModel> Suppliers { get; set; }
        public DbSet<CouponModel> Coupon { get; set; }
        public DbSet<KitchenModel> Kitchen { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            

            modelBuilder.Entity<MaterialModel>()
                .HasOne(m => m.Supplier)
                .WithMany(s=>s.Materials)
                .HasForeignKey(m => m.SupplierId)
                .OnDelete(DeleteBehavior.Restrict); // ⛔ NO CASCADE
        }


    }
}
