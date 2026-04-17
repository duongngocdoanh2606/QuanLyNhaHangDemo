using Microsoft.AspNetCore.Identity;

namespace QuanLyNhaHangDemo.Models
{
    public class AppUserModel :IdentityUser
    {
        public string Occupation { get; set; }

        public string RoleId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
