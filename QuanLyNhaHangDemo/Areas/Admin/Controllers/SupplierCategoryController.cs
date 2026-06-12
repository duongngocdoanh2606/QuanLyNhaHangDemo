using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Models;
using QuanLyNhaHangDemo.Repository;

[Area("Admin")]
public class SupplierCategoryController : Controller
{
    private readonly DataContext _context;

    public SupplierCategoryController(DataContext context)
    {
        _context = context;
    }

    // ================== INDEX ==================
    public async Task<IActionResult> Index()
    {
        var categories = await _context.SupplierCategories
            .Include(c => c.Suppliers)
            .ToListAsync();
        return View(categories);
    }

    // ================== CREATE ==================
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupplierCategoryModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        _context.SupplierCategories.Add(model);
        await _context.SaveChangesAsync();

        TempData["success"] = "Thêm danh mục nhà cung cấp thành công!";
        return RedirectToAction(nameof(Index));
    }

    // ================== EDIT ==================
    public async Task<IActionResult> Edit(int id)
    {
        var category = await _context.SupplierCategories.FindAsync(id);
        if (category == null) return NotFound();
        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SupplierCategoryModel model)
    {
        if (id != model.SupplierCategoryId) return NotFound();

        if (!ModelState.IsValid)
            return View(model);

        _context.SupplierCategories.Update(model);
        await _context.SaveChangesAsync();

        TempData["success"] = "Cập nhật danh mục nhà cung cấp thành công!";
        return RedirectToAction(nameof(Index));
    }

    // ================== DELETE ==================
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _context.SupplierCategories
            .Include(c => c.Suppliers)
            .FirstOrDefaultAsync(c => c.SupplierCategoryId == id);
        if (category == null) return NotFound();
        return View(category);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var category = await _context.SupplierCategories
            .Include(c => c.Suppliers)
            .FirstOrDefaultAsync(c => c.SupplierCategoryId == id);
        if (category == null) return NotFound();

        if (category.Suppliers != null && category.Suppliers.Any())
        {
            TempData["error"] = "Không thể xóa danh mục đang có nhà cung cấp!";
            return RedirectToAction(nameof(Index));
        }

        _context.SupplierCategories.Remove(category);
        await _context.SaveChangesAsync();

        TempData["success"] = "Xóa danh mục nhà cung cấp thành công!";
        return RedirectToAction(nameof(Index));
    }
}
