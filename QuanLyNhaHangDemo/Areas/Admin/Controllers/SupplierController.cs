using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Models;
using QuanLyNhaHangDemo.Models.ViewModels;
using QuanLyNhaHangDemo.Repository;
using System.Security.Cryptography.Xml;

[Area("Admin")]
public class SupplierController : Controller
{
    private readonly DataContext _context;

    public SupplierController(DataContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var suppliers = await _context.Suppliers
            .Include(s => s.Category)
            .ToListAsync();

        return View(suppliers);
    }

    public IActionResult Create()
    {
        var vm = new SupplierViewModel
        {
            Supplier = new SupplierModel(),
            Categories = GetCategories()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupplierViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Categories = GetCategories();
            return View(vm);
        }

        _context.Suppliers.Add(vm.Supplier);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null) return NotFound();

        var vm = new SupplierViewModel
        {
            Supplier = supplier,
            Categories = GetCategories()
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
            vm.Categories = GetCategories();
            return View(vm);
        }

        _context.Suppliers.Update(vm.Supplier);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // ================== DELETE ==================
    public async Task<IActionResult> Delete(int id)
    {
        var supplier = await _context.Suppliers
            .Include(s => s.Category)
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

        return RedirectToAction(nameof(Index));
    }

    // ================== HELPER ==================
    private List<SelectListItem> GetCategories()
    {
        return _context.Categories
            .Select(b => new SelectListItem
            {
                Value = b.Id.ToString(),
                Text = b.Name
            })
            .ToList();
    }
}
