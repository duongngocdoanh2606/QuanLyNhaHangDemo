using System.ComponentModel.DataAnnotations;

namespace QuanLyNhaHangDemo.Models.ViewModels
{
    public class ProfileViewModel
    {
        public string Id { get; set; }

        [Required]
        public string Username { get; set; }

        [EmailAddress]
        public string Email { get; set; }    // không cho sửa nếu muốn

        public string PhoneNumber { get; set; }

        public string Address { get; set; }
        

    }

}