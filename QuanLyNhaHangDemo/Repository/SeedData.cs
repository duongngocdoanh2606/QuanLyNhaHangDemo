using Microsoft.AspNetCore.Identity;
using System.Linq;
using QuanLyNhaHangDemo.Models;

namespace QuanLyNhaHangDemo.Repository
{
    public class SeedData
    {
        public static void SeedingData(DataContext _context)
        {
            // Ensure roles: "Admin" and "Customer"
            if (!_context.Roles.Any(r => r.Name == "Admin"))
            {
                var adminRole = new IdentityRole
                {
                    Name = "Admin",
                    NormalizedName = "ADMIN"
                };
                _context.Roles.Add(adminRole);
            }

            _context.SaveChanges();

            if (!_context.Users.Any(u => u.UserName == "admin"))
            {
                // TẠO TRƯỚC ID để dùng cho bảng trung gian
                var adminId = Guid.NewGuid().ToString();

                var adminUser = new AppUserModel
                {
                    Id = adminId, // Gán ID cố định ở đây
                    UserName = "admin",
                    NormalizedUserName = "ADMIN", // Cực kỳ quan trọng
                    Email = "admin@local",
                    NormalizedEmail = "ADMIN@LOCAL",
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString(), // Bắt buộc để không lỗi đăng nhập
                    Occupation = "Administrator"
                };

                var hasher = new PasswordHasher<AppUserModel>();
                adminUser.PasswordHash = hasher.HashPassword(adminUser, "admin123");

                _context.Users.Add(adminUser);

                var adminRoleId = _context.Roles.First(r => r.Name == "Admin").Id;


                _context.UserRoles.Add(new IdentityUserRole<string>
                {
                    RoleId = adminRoleId,
                    UserId = adminId // Dùng adminId đã tạo ở trên
                });

                _context.SaveChanges();
            }
        }
    }
}