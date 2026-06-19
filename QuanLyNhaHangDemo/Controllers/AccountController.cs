using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QuanLyNhaHangDemo.Models;
using QuanLyNhaHangDemo.Models.ViewModels;
using QuanLyNhaHangDemo.Repository;

namespace QuanLyNhaHangDemo.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUserModel> _userManager;
        private readonly SignInManager<AppUserModel> _signInManager;
        private readonly DataContext _dataContext;

        public AccountController(
            UserManager<AppUserModel> userManager,
            SignInManager<AppUserModel> signInManager,
            DataContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _dataContext = context;
        }

        // =====================================================
        // GET: /Account/Login
        // =====================================================
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            // Nếu đã login -> vào Admin Dashboard
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard",
                    new { area = "Admin" });
            }

            return View(new LoginViewModel
            {
                ReturnUrl = returnUrl
            });
        }

        // =====================================================
        // POST: /Account/Login
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel loginVM)
        {
            if (!ModelState.IsValid)
                return View(loginVM);

            // Tìm user
            var user = await _userManager.FindByNameAsync(loginVM.Username);

            if (user == null)
            {
                ModelState.AddModelError("", "Tài khoản không tồn tại.");
                return View(loginVM);
            }

            // Kiểm tra password
            var result = await _signInManager.PasswordSignInAsync(
                loginVM.Username,
                loginVM.Password,
                false,
                false);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Sai tài khoản hoặc mật khẩu.");
                return View(loginVM);
            }

            // Nếu có ReturnUrl
            if (!string.IsNullOrEmpty(loginVM.ReturnUrl))
            {
                return Redirect(loginVM.ReturnUrl);
            }

            // =====================================================
            // ROLE REDIRECT
            // =====================================================
            var roles = await _userManager.GetRolesAsync(user);

            // ADMIN
            if (roles.Any(r =>
                r.Equals("admin", StringComparison.OrdinalIgnoreCase)))
            {
                return RedirectToAction(
                    "Index",
                    "Category",
                    new { area = "Admin" });
            }

            // KITCHEN
            if (roles.Any(r =>
                r.Equals("kitchen", StringComparison.OrdinalIgnoreCase)))
            {
                return RedirectToAction(
                    "Index",
                    "Category",
                    new { area = "Admin" });
            }

            // WAREHOUSE
            if (roles.Any(r =>
                r.Equals("warehouse", StringComparison.OrdinalIgnoreCase)))
            {
                return RedirectToAction(
                    "Index",
                    "Category",
                    new { area = "Admin" });
            }

            // CASHIER
            if (roles.Any(r =>
                r.Equals("cashier", StringComparison.OrdinalIgnoreCase)))
            {
                return RedirectToAction(
                    "Index",
                    "Category",
                    new { area = "Admin" });
            }

            // STAFF
            if (roles.Any(r =>
                r.Equals("staff", StringComparison.OrdinalIgnoreCase)))
            {
                return RedirectToAction(
                    "Index",
                    "Category",
                    new { area = "Admin" });
            }

            // ROLE KHÁC
            return RedirectToAction(
                "Index",
                "Category",
                new { area = "Admin" });
        }

        // =====================================================
        // GET: /Account/Logout
        // =====================================================
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            // Logout Identity
            await _signInManager.SignOutAsync();

            // Clear Session
            HttpContext.Session.Clear();

            // Redirect về Login
            return RedirectToAction(
                "Login",
                "Account");
        }

    }
}