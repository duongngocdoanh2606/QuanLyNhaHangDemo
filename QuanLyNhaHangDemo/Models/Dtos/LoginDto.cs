using System.ComponentModel.DataAnnotations;

namespace QuanLyNhaHangDemo.Models.Dtos
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        public string Password { get; set; }
    }
    public class RegisterDto
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        public string username { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone(ErrorMessage = "Định dạng số điện thoại không hợp lệ")]
        public string phoneNumber { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        // Xóa dòng Compare ở đây vì nó phải nằm ở ô xác nhận
        public string password { get; set; }

        [Required(ErrorMessage = "Xác nhận mật khẩu không được để trống")]
        [Compare("password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string confirmPassword { get; set; }
    }
    public class LoginResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Token { get; set; }
        public string Username { get; set; }
    }
    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone(ErrorMessage = "Định dạng số điện thoại không hợp lệ")]
        public string phoneNumber { get; set; }
    }
    public class  ResetPasswordDto
    {
        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone(ErrorMessage = "Định dạng số điện thoại không hợp lệ")]
        public string phoneNumber { get; set; }

        [Required(ErrorMessage = "Mật khẩu mới không được để trống")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự")]
        public string newPassword { get; set; }
    }
    
}
