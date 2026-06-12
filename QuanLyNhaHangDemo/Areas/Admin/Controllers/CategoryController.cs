using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Models;
using QuanLyNhaHangDemo.Repository;

namespace QuanLyNhaHangDemo.Areas.Admin.Controllers
{
    [Area("Admin")]
    
    public class CategoryController:Controller
    {
        private readonly DataContext _dataContext;
        public CategoryController(DataContext context)
        {
            _dataContext = context;
            
        }
        
        public async Task<IActionResult> Index()
        {
            return View(await _dataContext.Categories.OrderByDescending(p => p.Id).Include(p=>p.Kitchen).ToListAsync());
        }


        [HttpGet]
        public IActionResult Create()
        {
            var kitchen = _dataContext.Kitchen
                .OrderBy(k => k.Name)
                .ToList();
            ViewBag.Kitchens = new SelectList(kitchen, "Id", "Name");
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryModel category)
        {
            ViewBag.Kitchens = await _dataContext.Kitchen
                .OrderBy(k => k.Name)
                .Select(k => new SelectListItem
                {
                    Value = k.Id.ToString(),
                    Text = k.Name
                })
                .ToListAsync();
            ModelState.Remove("Kitchen");
            ModelState.Remove("Products");

            if (ModelState.IsValid)
            {

                category.Slug = category.Name.Replace(" ", "-");
                var slug = await _dataContext.Categories.FirstOrDefaultAsync(p => p.Slug == category.Slug);
                if (slug != null)
                {
                    ModelState.AddModelError("", "Danh muc da co trong database");
                    return View(category);
                }
                _dataContext.Add(category);
                await _dataContext.SaveChangesAsync();
                TempData["success"] = "Them danh muc thanh cong";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["error"] = "Model co mot vai thu dang bi loi";
                List<string> errors = new List<string>();
                foreach (var value in ModelState.Values)
                {
                    foreach (var error in value.Errors)
                    {
                        errors.Add(error.ErrorMessage);
                    }
                }
                string errorMessage = string.Join("\n", errors);
                return BadRequest(errorMessage);
            }

            return View(category);
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            var kitchen = _dataContext.Kitchen
                .OrderBy(k => k.Name)
                .ToList();
            ViewBag.Kitchens = new SelectList(kitchen, "Id", "Name");
            CategoryModel category = await _dataContext.Categories.FindAsync(Id);
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CategoryModel category)
        {
            ModelState.Remove("Kitchen");
            ModelState.Remove("Products");

            if (ModelState.IsValid)
            {
                category.Slug = category.Name.Replace(" ", "-");

                // ✅ Sửa 1: loại trừ chính bản ghi hiện tại
                var slug = await _dataContext.Categories
                    .FirstOrDefaultAsync(p => p.Slug == category.Slug && p.Id != category.Id);
                // ✅ Sửa 2: Lấy từ DB để update, không dùng _dataContext.Update(category)
                var existingCategory = await _dataContext.Categories.FindAsync(category.Id);
                if (existingCategory == null)
                {
                    return NotFound();
                }

                existingCategory.Name = category.Name;
                existingCategory.Slug = category.Slug;
                existingCategory.Description = category.Description;
                existingCategory.Status = category.Status;
                existingCategory.Priority = category.Priority;
                existingCategory.KitchenId = category.KitchenId;
                await _dataContext.SaveChangesAsync();
                TempData["success"] = "Cập nhật danh mục thành công";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["error"] = "Model có một vài lỗi";
                List<string> errors = new List<string>();
                foreach (var value in ModelState.Values)
                {
                    foreach (var error in value.Errors)
                    {
                        errors.Add(error.ErrorMessage);
                    }
                }
                string errorMessage = string.Join("\n", errors);
                return BadRequest(errorMessage);
            }

            return View(category);
        }

        public async Task<IActionResult> Delete(int Id)
        {
            CategoryModel category = await _dataContext.Categories.FindAsync(Id);   
            _dataContext.Categories.Remove(category);
            await _dataContext.SaveChangesAsync();
            TempData["success"] = "Đã xóa danh mục";
            return RedirectToAction("Index");
        }
        [HttpPost]
        public IActionResult UpdateAutoFire(int id,bool status)
        {
            var category = _dataContext.Categories.Find(id);
            if(category != null)
            {
                category.isAutoFire = status;
                _dataContext.SaveChanges();
                return Json(new { success = true, message = "Cập nhật thành công" });
            }
            return Json(new { success = false, message = "Không tìm thấy danh mục" });
        }
    }
}
