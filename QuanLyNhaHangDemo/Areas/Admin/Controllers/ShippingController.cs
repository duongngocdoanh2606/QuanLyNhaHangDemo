using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Models;
using QuanLyNhaHangDemo.Repository;
using System.Threading.Tasks;

namespace QuanLyNhaHangDemo.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ShippingController : Controller
    {
        private readonly DataContext _dataContext;

        public ShippingController(DataContext context)
        {
            _dataContext = context;
        }

        public async Task<IActionResult> Index()
        {
            var shippingList = await _dataContext.Shippings.ToListAsync();
            ViewBag.ShippingList = shippingList;
            return View();
        }

        [HttpPost]
        [Route("StoreShipping")]
        public async Task<IActionResult> StoreShipping(ShippingModel shippingModel, string tinh, string quan, decimal price)
        {
            shippingModel.City = tinh;
            shippingModel.District = quan;
            shippingModel.Price = price;
            try
            {
                var existingShipping = await _dataContext.Shippings
                    .AnyAsync(s => s.City == tinh && s.District == quan);
                if (existingShipping)
                {
                    return Ok(new { duplicate = true, message = "Du lieu trung" });

                }
                _dataContext.Shippings.Add(shippingModel);
                await _dataContext.SaveChangesAsync();
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = shippingModel.Id,
                        city = shippingModel.City,
                        district = shippingModel.District,
                        price = shippingModel.Price.ToString("N0")
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error");

            }
        }
        public async Task<IActionResult> Delete(int id)
        {
            ShippingModel shipping = await _dataContext.Shippings.FindAsync(id);
            _dataContext.Shippings.Remove(shipping);
            await _dataContext.SaveChangesAsync();
            TempData["success"] = "Shipping đã được xóa thành công";
            return RedirectToAction("Index","Shipping");
        }
    }
}
