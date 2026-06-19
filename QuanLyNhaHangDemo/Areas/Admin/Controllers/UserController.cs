using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Models;
using QuanLyNhaHangDemo.Repository;


namespace QuanLyNhaHangDemo.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/User")]
    
    public class UserController : Controller
    {
        private readonly UserManager<AppUserModel> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly DataContext _dataContext;



        public UserController(DataContext context,
            UserManager<AppUserModel> userManager,
            RoleManager<IdentityRole> roleManager
            )
        {

            _userManager = userManager;
            _roleManager = roleManager;
            _dataContext = context;

        }
        [HttpGet]
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            var usersWithRoles = await (from u in _dataContext.Users
                                        join ur in _dataContext.UserRoles on u.Id equals ur.UserId into userRoles
                                        from ur in userRoles.DefaultIfEmpty()
                                        join r in _dataContext.Roles on ur.RoleId equals r.Id into roles
                                        from r in roles.DefaultIfEmpty()
                                        select new { User = u, RoleName = r != null ? r.Name : "Chưa có quyền" })
                   .ToListAsync();


            return View(usersWithRoles);
        }
        [HttpGet]
        [Route("Create")]
        public async Task<IActionResult> Create()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "Id", "Name");
            return View(new AdminCreateUserViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Create")]
        public async Task<IActionResult> Create(AdminCreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var rolesList = await _roleManager.Roles.ToListAsync();
                ViewBag.Roles = new SelectList(rolesList, "Id", "Name", model.RoleId);
                return View(model);
            }

            // 1) Tạo user nội bộ
            var user = new AppUserModel
            {
                UserName = model.UserName,
                EmailConfirmed = true   // user nội bộ: không cần xác nhận email
            };

            // 2) Gán email kỹ thuật, luôn hợp lệ & luôn duy nhất
            //    ví dụ: username@internal.local, nếu trùng thì thêm số
            string baseEmail = $"{model.UserName}@internal.local";
            string internalEmail = baseEmail;
            int counter = 1;

            while (await _userManager.FindByEmailAsync(internalEmail) != null)
            {
                internalEmail = $"{model.UserName}+{counter}@internal.local";
                counter++;
            }

            user.Email = internalEmail;

            // 3) Tạo user với mật khẩu
            var createUserResult = await _userManager.CreateAsync(user, model.Password);
            if (!createUserResult.Succeeded)
            {
                AddIdentityErrors(createUserResult);
                var rolesList = await _roleManager.Roles.ToListAsync();
                ViewBag.Roles = new SelectList(rolesList, "Id", "Name", model.RoleId);
                return View(model);
            }

            // 4) Gán role
            var role = await _roleManager.FindByIdAsync(model.RoleId);
            if (role != null)
            {
                var addToRoleResult = await _userManager.AddToRoleAsync(user, role.Name);
                if (!addToRoleResult.Succeeded)
                {
                    AddIdentityErrors(addToRoleResult);
                    var rolesList = await _roleManager.Roles.ToListAsync();
                    ViewBag.Roles = new SelectList(rolesList, "Id", "Name", model.RoleId);
                    return View(model);
                }
            }

            TempData["success"] = "Tạo user nội bộ thành công.";
            return RedirectToAction("Index");
        }

        private void AddIdentityErrors(IdentityResult identityResult)
        {
            foreach (var error in identityResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }


        [HttpGet]
        [Route("Edit")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            // Lấy danh sách role
            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "Id", "Name");

            // Lấy role hiện tại của user qua UserManager
            var userRoles = await _userManager.GetRolesAsync(user);
            IdentityRole? currentRole = null;
            if (userRoles.Any())
            {
                var roleName = userRoles.First();
                currentRole = await _roleManager.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            }

            var model = new EditUserViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                RoleId = currentRole?.Id ?? ""  // nếu chưa có quyền thì để trống
            };
            
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Edit")]
        public async Task<IActionResult> Edit(string id, EditUserViewModel model)
        {
            if (string.IsNullOrEmpty(id) || id != model.Id)
                return NotFound();

            var existingUser = await _userManager.FindByIdAsync(id);
            if (existingUser == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                var rolesList = await _roleManager.Roles.ToListAsync();
                ViewBag.Roles = new SelectList(rolesList, "Id", "Name", model.RoleId);
                TempData["error"] = "Model validation failed.";
                return View(model);
            }

            // 1. Cập nhật thông tin cơ bản
            existingUser.UserName = model.UserName;
            existingUser.Email = model.Email;
            existingUser.PhoneNumber = model.PhoneNumber;

            var updateUserResult = await _userManager.UpdateAsync(existingUser);
            if (!updateUserResult.Succeeded)
            {
                AddIdentityErrors(updateUserResult);
                var rolesList = await _roleManager.Roles.ToListAsync();
                ViewBag.Roles = new SelectList(rolesList, "Id", "Name", model.RoleId);
                return View(model);
            }

            // 2. Cập nhật Role (xóa role cũ, gán role mới)
            var currentRoles = await _userManager.GetRolesAsync(existingUser);
            if (currentRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(existingUser, currentRoles);
                if (!removeResult.Succeeded)
                {
                    AddIdentityErrors(removeResult);
                    var rolesList = await _roleManager.Roles.ToListAsync();
                    ViewBag.Roles = new SelectList(rolesList, "Id", "Name", model.RoleId);
                    return View(model);
                }
            }

            var newRole = await _roleManager.FindByIdAsync(model.RoleId);
            if (newRole != null)
            {
                var addRoleResult = await _userManager.AddToRoleAsync(existingUser, newRole.Name);
                if (!addRoleResult.Succeeded)
                {
                    AddIdentityErrors(addRoleResult);
                    var rolesList = await _roleManager.Roles.ToListAsync();
                    ViewBag.Roles = new SelectList(rolesList, "Id", "Name", model.RoleId);
                    return View(model);
                }
            }

            // 3. Đổi mật khẩu nếu có nhập mật khẩu mới
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
                var resetResult = await _userManager.ResetPasswordAsync(existingUser, resetToken, model.NewPassword);

                if (!resetResult.Succeeded)
                {
                    AddIdentityErrors(resetResult);
                    var rolesList = await _roleManager.Roles.ToListAsync();
                    ViewBag.Roles = new SelectList(rolesList, "Id", "Name", model.RoleId);
                    return View(model);
                }
            }

            TempData["success"] = "Cập nhật người dùng thành công.";
            return RedirectToAction("Index", "User");
        }

        [HttpGet]
        [Route("Delete")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            var deleteResult = await _userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                return View("Error");
            }
            TempData["success"] = "User đã được xóa thành công";
            return RedirectToAction("Index");
        }

    }
}