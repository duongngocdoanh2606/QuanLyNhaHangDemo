using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Areas.Admin.Repository;
using QuanLyNhaHangDemo.Models;
using QuanLyNhaHangDemo.Models.ViewModels;
using QuanLyNhaHangDemo.Repository;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace QuanLyNhaHangDemo.Controllers
{
    public class AccountController : Controller
    {
        private UserManager<AppUserModel> _userManage;
        public SignInManager<AppUserModel> _signInManager;
        private readonly DataContext _dataContext;
        private readonly IEmailSender _emailSender;
        public AccountController(IEmailSender emailsender, SignInManager<AppUserModel> signInManager, UserManager<AppUserModel> userManage, DataContext context)
        {
            _userManage = userManage;
            _signInManager = signInManager;
            _dataContext = context;
            _emailSender = emailsender;
        }
        public IActionResult Login(string returnUrl)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel loginVM)
        {
            if (!ModelState.IsValid)
                return View(loginVM);

            var user = await _userManage.FindByNameAsync(loginVM.Username);

            if (user != null)
            {
                var roles = await _userManage.GetRolesAsync(user);

                // ✳ CHỈ kiểm tra với role Customer
                if (roles.Any(r => string.Equals(r, "Customer", StringComparison.OrdinalIgnoreCase)) &&
                    !await _userManage.IsEmailConfirmedAsync(user))
                {
                    ModelState.AddModelError("", "Bạn cần xác nhận email trước khi đăng nhập.");
                    return View(loginVM);
                }
            }

            var result = await _signInManager.PasswordSignInAsync(loginVM.Username, loginVM.Password, false, false);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Sai tên đăng nhập hoặc mật khẩu.");
                return View(loginVM);
            }

            // Redirect theo ReturnUrl hoặc theo role (admin/kitchen/...)
            if (!string.IsNullOrEmpty(loginVM.ReturnUrl))
                return Redirect(loginVM.ReturnUrl);

            if (user != null)
            {
                var roles2 = await _userManage.GetRolesAsync(user);
                if (roles2.Any(r => r.Equals("kitchen", StringComparison.OrdinalIgnoreCase)))
                    return RedirectToAction("Index", "Kitchen", new { area = "Admin" });

                if (roles2.Any(r => r.Equals("admin", StringComparison.OrdinalIgnoreCase)))
                    return RedirectToAction("Index", "Home", new { area = "Admin" });
            }

            return Redirect("/");
        }



        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create(UserModel user)
        {
            if (!ModelState.IsValid)
                return View(user);

            var newUser = new AppUserModel
            {
                UserName = user.Username,
                Email = user.Email
                // EmailConfirmed mặc định = false
            };

            var result = await _userManage.CreateAsync(newUser, user.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(user);
            }

            // Gán role CUSTOMER
            await _userManage.AddToRoleAsync(newUser, "Customer");

            // Tạo token + link xác nhận
            var token = await _userManage.GenerateEmailConfirmationTokenAsync(newUser);
            var confirmationLink = Url.Action(
                "ConfirmEmail",
                "Account",
                new { userId = newUser.Id, token = token },
                protocol: HttpContext.Request.Scheme);

            var subject = "Xác nhận tài khoản";
            var message = $@"
    <div style='font-family: Arial, sans-serif; font-size:14px; color:#333;'>
        <h2 style='color:#2c3e50; margin-bottom:10px;'>Chào {newUser.UserName},</h2>
        <p style='margin:0 0 15px 0;'>
            Cảm ơn bạn đã đăng ký tài khoản. Vui lòng nhấn vào nút bên dưới để xác nhận email và kích hoạt tài khoản:
        </p>

        <p style='margin:20px 0; text-align:center;'>
            <a href='{confirmationLink}'
               style='display:inline-block;
                      padding:12px 24px;
                      background-color:#28a745;
                      color:#ffffff !important;
                      text-decoration:none;
                      border-radius:6px;
                      font-weight:bold;
                      font-size:14px;'>
                XÁC NHẬN TÀI KHOẢN
            </a>
        </p>

        <p style='font-size:12px; color:#777; margin-top:20px;'>
            Nếu nút trên không hoạt động, bạn có thể copy đường dẫn sau và dán vào trình duyệt:<br />
            <span style='word-break:break-all;'>{confirmationLink}</span>
        </p>

        <p style='font-size:12px; color:#999; margin-top:10px;'>
            Trân trọng,<br/>
            Hệ thống đặt bàn / nhà hàng
        </p>
    </div>";
            ;

            await _emailSender.SendEmailAsync(newUser.Email, subject, message);

            ViewBag.Info = "Đăng ký thành công. Vui lòng kiểm tra email để xác nhận tài khoản.";
            user.Password = string.Empty;
            return View(user);
        }



        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
            {
                TempData["Success"] = "Thiếu thông tin xác nhận.";
                return RedirectToAction("Login");
            }

            var user = await _userManage.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Success"] = "Không tìm thấy tài khoản.";
                return RedirectToAction("Login");
            }

            var result = await _userManage.ConfirmEmailAsync(user, token);

            if (result.Succeeded)
                TempData["Success"] = "Email đã được xác nhận. Bạn có thể đăng nhập.";
            else
                TempData["Success"] = "Xác nhận email thất bại. Link có thể đã hết hạn.";

            return RedirectToAction("Login");
        }
        public async Task<IActionResult> Logout(string returnUrl = "/")
        {
            await _signInManager.SignOutAsync();
            return Redirect(returnUrl);
        }
        public async Task<IActionResult> History()
        {
            if ((bool)!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var Orders = await _dataContext.Orders.Where(o => o.UserName == userEmail).ToListAsync();
            ViewBag.UserEmail = userEmail;
            return View(Orders);
        }
        public async Task<IActionResult> CancelOrder(string ordercode)
        {
            if ((bool)!User.Identity?.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var order = await _dataContext.Orders
                    .FirstOrDefaultAsync(o => o.OrderCode == ordercode);

                if (order == null) return NotFound();

                // trạng thái đơn = ĐÃ HỦY (vd: 3)
                order.Status = 3;

                // 🔴 LẤY TẤT CẢ MÓN TRONG ĐƠN VÀ ĐÁNH DẤU HỦY
                var details = await _dataContext.OrderDetails
                    .Where(d => d.OrderCode == ordercode)
                    .ToListAsync();

                foreach (var d in details)
                {
                    // quy ước: 3 = Hủy món
                    d.Status = (StatusProduct)3;
                }

                _dataContext.Orders.Update(order);
                _dataContext.OrderDetails.UpdateRange(details);

                await _dataContext.SaveChangesAsync();
            }
            catch (Exception)
            {
                return BadRequest("An error");
            }

            return RedirectToAction("History", "Account");
        }


        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManage.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Không tiết lộ user có tồn tại hay không
                TempData["ForgotPasswordInfo"] = "Nếu email tồn tại trong hệ thống, chúng tôi đã gửi hướng dẫn đặt lại mật khẩu.";
                return RedirectToAction("Login");
            }

            // (tuỳ chọn) chỉ cho Customer reset bằng luồng này
            var roles = await _userManage.GetRolesAsync(user);
            if (!roles.Any(r => r.Equals("Customer", StringComparison.OrdinalIgnoreCase)))
            {
                TempData["ForgotPasswordInfo"] = "Nếu email tồn tại trong hệ thống, chúng tôi đã gửi hướng dẫn đặt lại mật khẩu.";
                return RedirectToAction("Login");
            }

            // token reset password
            var token = await _userManage.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.Action(
                "ResetPassword",
                "Account",
                new { email = model.Email, token = token },
                protocol: HttpContext.Request.Scheme);

            var subject = "Đặt lại mật khẩu";
            var message = $@"
        <div style='font-family: Arial, sans-serif; font-size:14px; color:#333;'>
            <h2 style='color:#2c3e50; margin-bottom:10px;'>Xin chào {user.UserName},</h2>
            <p style='margin:0 0 15px 0;'>
                Bạn (hoặc ai đó) đã yêu cầu đặt lại mật khẩu cho tài khoản của bạn.
                Nếu đó là bạn, vui lòng nhấn nút bên dưới:
            </p>

            <p style='margin:20px 0; text-align:center;'>
                <a href='{resetLink}'
                   style='display:inline-block;
                          padding:12px 24px;
                          background-color:#007bff;
                          color:#ffffff !important;
                          text-decoration:none;
                          border-radius:6px;
                          font-weight:bold;
                          font-size:14px;'>
                    ĐẶT LẠI MẬT KHẨU
                </a>
            </p>

            <p style='font-size:12px; color:#777; margin-top:20px;'>
                Nếu nút trên không hoạt động, hãy copy đường dẫn sau và dán vào trình duyệt:<br />
                <span style='word-break:break-all;'>{resetLink}</span>
            </p>

            <p style='font-size:12px; color:#999; margin-top:10px;'>
                Nếu bạn không yêu cầu đặt lại mật khẩu, có thể bỏ qua email này.
            </p>
        </div>";

            await _emailSender.SendEmailAsync(model.Email, subject, message);

            TempData["ForgotPasswordInfo"] = "Nếu email tồn tại trong hệ thống, chúng tôi đã gửi hướng dẫn đặt lại mật khẩu.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                TempData["ResetError"] = "Link đặt lại mật khẩu không hợp lệ.";
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordViewModel
            {
                Email = email,
                Token = token
            };

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManage.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Không tiết lộ user tồn tại hay không
                TempData["ResetSuccess"] = "Mật khẩu đã được đặt lại (nếu tài khoản tồn tại).";
                return RedirectToAction("Login");
            }

            var result = await _userManage.ResetPasswordAsync(user, model.Token, model.Password);

            if (result.Succeeded)
            {
                TempData["ResetSuccess"] = "Đặt lại mật khẩu thành công. Bạn có thể đăng nhập với mật khẩu mới.";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }
        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManage.FindByIdAsync(userId);

            if (user == null) return NotFound();

            var model = new ProfileViewModel
            {
                Id = user.Id,
                Username = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManage.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            user.UserName = model.Username;
            user.PhoneNumber = model.PhoneNumber;

            var result = await _userManage.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["success"] = "Cập nhật thông tin thành công!";
                return RedirectToAction("Profile");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }
        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManage.FindByIdAsync(userId);

            if (user == null) return NotFound();

            var result = await _userManage.ChangePasswordAsync(
                user,
                model.CurrentPassword,
                model.NewPassword
            );

            if (result.Succeeded)
            {
                TempData["success"] = "Đổi mật khẩu thành công!";
                return RedirectToAction("Profile");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }


    }
}

