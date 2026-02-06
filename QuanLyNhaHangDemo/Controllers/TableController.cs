using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Models;
using QuanLyNhaHangDemo.Repository;
using Microsoft.AspNetCore.Http;

namespace QuanLyNhaHangDemo.Controllers
{
    public class TableController : Controller
    {
        private readonly DataContext _context;

        public TableController(DataContext context)
        {
            _context = context;
        }

        // /Table/Index – hiển thị sơ đồ bàn
        public async Task<IActionResult> Index()
        {
            var tables = await _context.Table
                                       .OrderBy(t => t.Id)
                                       .ToListAsync();
            return View(tables);
        }

        public async Task<IActionResult> Choose(int tableId)
        {
            var table = await _context.Table.FindAsync(tableId);
            if (table == null) return NotFound();

            HttpContext.Session.SetInt32("CurrentTableId", tableId);

            if (table.Status == TableStatus.Empty)
            {
                table.Status = TableStatus.Serving;
                _context.Table.Update(table);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", "Home");
        }
        
    }
}
