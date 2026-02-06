using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Models;
using QuanLyNhaHangDemo.Repository;
using System.Threading.Tasks;

namespace QuanLyNhaHangDemo.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CouponController : Controller
    {
        private readonly DataContext _dataContext;
        public CouponController(DataContext context)
        {
            _dataContext = context;
        }
        public async Task<IActionResult> Index()
        {
            var coupons = await _dataContext.Coupon.ToListAsync();
            return View(coupons);
        }
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create(CouponModel coupon)
        {
            if (coupon.EndDate < coupon.StartDate)
            {
                ModelState.AddModelError("", "Ngày kết thúc phải lớn hơn ngày bắt đầu");
            }

            if (_dataContext.Coupon.Any(x => x.Code == coupon.Code))
            {
                ModelState.AddModelError("", "Coupon code đã tồn tại");
            }

            if (ModelState.IsValid)
            {
                if (coupon.DiscountType == DiscountTypeEnum.FixedAmount)
                {
                    coupon.MaxDiscountAmount = null;
                }

                _dataContext.Coupon.Add(coupon);
                await _dataContext.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return View(coupon);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(CouponModel coupon)
        {
            if (coupon.EndDate < coupon.StartDate)
            {
                ModelState.AddModelError("", "Ngày kết thúc phải lớn hơn ngày bắt đầu");
            }

            if (ModelState.IsValid)
            {
                if (coupon.DiscountType == DiscountTypeEnum.FixedAmount)
                {
                    coupon.MaxDiscountAmount = null;
                }

                _dataContext.Coupon.Update(coupon);
                await _dataContext.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return View(coupon);
        }

        // DELETE
        public async Task<IActionResult> Delete(int id)
        {
            var coupon = await _dataContext.Coupon.FindAsync(id);
            if (coupon != null)
            {
                _dataContext.Coupon.Remove(coupon);
                await _dataContext.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }
    }
}
