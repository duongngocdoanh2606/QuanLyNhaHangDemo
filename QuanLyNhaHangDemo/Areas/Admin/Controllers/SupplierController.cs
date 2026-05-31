using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Models;
using QuanLyNhaHangDemo.Models.ViewModels;
using QuanLyNhaHangDemo.Repository;

[Area("Admin")]
public class SupplierController : Controller
{
    private readonly DataContext _context;

    public SupplierController(DataContext context)
    {
        _context = context;
    }

    // ================== INDEX ==================
    public async Task<IActionResult> Index()
    {
        var suppliers = await _context.Suppliers
            .Include(s => s.SupplierCategory)
            .ToListAsync();

        return View(suppliers);
    }

    // ================== CREATE ==================
    public IActionResult Create()
    {
        var vm = new SupplierViewModel
        {
            Supplier = new SupplierModel(),
            SupplierCategories = GetSupplierCategories()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupplierViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.SupplierCategories = GetSupplierCategories();
            return View(vm);
        }

        _context.Suppliers.Add(vm.Supplier);
        await _context.SaveChangesAsync();

        TempData["success"] = "Thêm nhà cung cấp thành công!";
        return RedirectToAction(nameof(Index));
    }

    // ================== EDIT ==================
    public async Task<IActionResult> Edit(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null) return NotFound();

        var vm = new SupplierViewModel
        {
            Supplier = supplier,
            SupplierCategories = GetSupplierCategories()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SupplierViewModel vm)
    {
        if (id != vm.Supplier.SupplierId)
            return NotFound();

        if (!ModelState.IsValid)
        {
            vm.SupplierCategories = GetSupplierCategories();
            return View(vm);
        }

        _context.Suppliers.Update(vm.Supplier);
        await _context.SaveChangesAsync();

        TempData["success"] = "Cập nhật nhà cung cấp thành công!";
        return RedirectToAction(nameof(Index));
    }

    // ================== DELETE ==================
    public async Task<IActionResult> Delete(int id)
    {
        var supplier = await _context.Suppliers
            .Include(s => s.SupplierCategory)
            .FirstOrDefaultAsync(s => s.SupplierId == id);

        if (supplier == null) return NotFound();
        return View(supplier);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null) return NotFound();

        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync();

        TempData["success"] = "Xóa nhà cung cấp thành công!";
        return RedirectToAction(nameof(Index));
    }

    // ================== HELPER ==================
    private List<SelectListItem> GetSupplierCategories()
    {
        return _context.SupplierCategories
            .Where(c => c.IsActive)
            .Select(c => new SelectListItem
            {
                Value = c.SupplierCategoryId.ToString(),
                Text = c.Name
            })
            .ToList();
    }
}
