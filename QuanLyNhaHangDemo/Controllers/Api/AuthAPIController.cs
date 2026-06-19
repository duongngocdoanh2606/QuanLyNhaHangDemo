using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Models;
using QuanLyNhaHangDemo.Models.Dtos;
using System;

[Route("api/[controller]")]
[ApiController]
public class AuthAPIController : ControllerBase
{
    private readonly UserManager<AppUserModel> _userManager;
    private readonly SignInManager<AppUserModel> _signInManager;

    public AuthAPIController(UserManager<AppUserModel> userManager,
                             SignInManager<AppUserModel> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        var user = await _userManager.FindByNameAsync(loginDto.Username);
        if (user == null)
        {
            return Unauthorized(new { Success = false, Message = "Sai tên đăng nhập hoặc mật khẩu." });
        }
        var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

        if (result.Succeeded)
        {
            // Lấy roles để phân quyền
            var roles = await _userManager.GetRolesAsync(user);

            // Chặn Kitchen, Cashier, Warehouse đăng nhập vào app Android
            if (roles.Any(r => r.Equals("kitchen", StringComparison.OrdinalIgnoreCase) || 
                               r.Equals("cashier", StringComparison.OrdinalIgnoreCase) || 
                               r.Equals("warehouse", StringComparison.OrdinalIgnoreCase)))
            {
                await _signInManager.SignOutAsync();
                return Unauthorized(new { Success = false, Message = "Tài khoản của bạn không được phép đăng nhập trên ứng dụng di động." });
            }

            return Ok(new
            {
                Success = true,
                Message = "Đăng nhập thành công.",
                Username = user.UserName,
                Roles = roles
            });
        }
        return Unauthorized(new { Success = false, Message = "Sai tên đăng nhập hoặc mật khẩu." });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userByPhone = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == registerDto.phoneNumber);
        if (userByPhone != null) return BadRequest(new { Success = false, Message = "Số điện thoại đã được đăng ký." });

        var newUser = new AppUserModel
        {
            UserName = registerDto.username,
            PhoneNumber = registerDto.phoneNumber,
            Email = registerDto.phoneNumber + "@phone.com",
            CreatedAt = DateTime.Now,
            PhoneNumberConfirmed = true
        };

        var result = await _userManager.CreateAsync(newUser, registerDto.password);

        if (result.Succeeded)
        {
          
            await _userManager.AddToRoleAsync(newUser, "Customer");
            return Ok(new { Success = true, Message = "Đăng ký thành công. Hãy xác thực otp số điện thoại." });
        }

        return BadRequest(new { Success = false, Errors = result.Errors.Select(e => e.Description) });
    }

    [HttpPost("reset-password-otp")]
    public async Task<IActionResult> ResetPasswordOTP([FromBody] ResetPasswordDto model)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == model.phoneNumber);
        if (user == null) return BadRequest(new { Success = false, Message = "Số điện thoại không tồn tại." });
        var removeResult = await _userManager.RemovePasswordAsync(user);
        if (removeResult.Succeeded)
        {
            var addResult = await _userManager.AddPasswordAsync(user, model.newPassword);
            if (addResult.Succeeded)
            {
                return Ok(new { Success = true, Message = "Cập nhật mật khẩu thành công." });
            }
        }
        return BadRequest(new { Success = false, Message = "Lỗi khi cập nhật mật khẩu." });
    }
}