using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Models;
using QuanLyNhaHangDemo.Repository;

namespace QuanLyNhaHangDemo.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class TableAdminController : Controller
    {
        private readonly DataContext _context;

        public TableAdminController(DataContext context)
        {
            _context = context;
        }

        // GET: Admin/Table
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var tables = await _context.Table.ToListAsync();
            return View(tables);
        }

        // GET: Admin/Table/Create
        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Table/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TableModel table)
        {
            if (!ModelState.IsValid)
                return View(table);

            table.Status = TableStatus.Empty;

            _context.Table.Add(table);
            TempData["success"] = "Thêm bàn thành công";
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // GET: Admin/Table/Edit/5
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var table = await _context.Table.FindAsync(id);
            if (table == null) return NotFound();

            return View(table);
        }

        // POST: Admin/Table/Edit
        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TableModel table)
        {
            if (id != table.Id) return NotFound();

            if (!ModelState.IsValid)
                return View(table);

            _context.Update(table);
            TempData["success"] = "Cập nhật bàn thành công";
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // Delete
        [HttpGet("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var table = await _context.Table.FindAsync(id);
            if (table == null) return NotFound();

            _context.Table.Remove(table);
            TempData["success"] = "Xóa bàn thành công";
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
