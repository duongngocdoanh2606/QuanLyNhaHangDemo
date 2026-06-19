using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Models;
using QuanLyNhaHangDemo.Repository;
using System.Threading.Tasks;

namespace QuanLyNhaHangDemo.Controllers
{
    [Area("Admin")]
    public class CouponController : Controller
    {
        private readonly DataContext _context;

        public CouponController(DataContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. READ: Danh sách mã giảm giá
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var coupons = await _context.Coupons.ToListAsync();
            return View(coupons);
        }

        // ==========================================
        // 2. CREATE: Thêm mới mã giảm giá
        // ==========================================
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CouponModel coupon)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra trùng mã
                bool isExist = await _context.Coupons
                    .AnyAsync(c => c.Code.ToUpper() == coupon.Code.ToUpper());

                if (isExist)
                {
                    ModelState.AddModelError("Code", "Mã giảm giá này đã tồn tại!");
                    return View(coupon);
                }

                _context.Add(coupon);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm mới mã giảm giá thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(coupon);
        }

        // ==========================================
        // 3. UPDATE: Sửa mã giảm giá
        // ==========================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null) return NotFound();

            return View(coupon);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CouponModel coupon)
        {
            if (id != coupon.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra xem mã sửa đổi có trùng với mã KHÁC trong DB không
                    bool isExist = await _context.Coupons
                        .AnyAsync(c => c.Code.ToUpper() == coupon.Code.ToUpper() && c.Id != id);

                    if (isExist)
                    {
                        ModelState.AddModelError("Code", "Mã giảm giá này đã tồn tại ở một bản ghi khác!");
                        return View(coupon);
                    }

                    _context.Update(coupon);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật mã giảm giá thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CouponExists(coupon.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(coupon);
        }

        // ==========================================
        // 4. DELETE: Xóa mã giảm giá
        // ==========================================
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var coupon = await _context.Coupons.FirstOrDefaultAsync(m => m.Id == id);
            if (coupon == null) return NotFound();

            return View(coupon);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon != null)
            {
                _context.Coupons.Remove(coupon);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa mã giảm giá thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool CouponExists(int id)
        {
            return _context.Coupons.Any(e => e.Id == id);
        }
    }
}