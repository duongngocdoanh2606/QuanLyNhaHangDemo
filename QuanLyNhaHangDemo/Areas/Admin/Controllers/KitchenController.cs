using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Models; // nếu cần
using QuanLyNhaHangDemo.Repository;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace QuanLyNhaHangDemo.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class KitchenController : Controller
    {
        private readonly DataContext _dataContext;

        public KitchenController(DataContext context)
        {
            _dataContext = context;
        }

        // nhận optional selectedKitchenId để chọn tab đúng khi quay về
        public async Task<IActionResult> Index(int? selectedKitchenId)
        {
            var kitchen = await _dataContext.Kitchen.OrderBy(k => k.SortOrder).ToListAsync();
            ViewBag.SelectedKitchenId = selectedKitchenId ?? (kitchen.FirstOrDefault()?.Id ?? 0);
            return View(kitchen);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(KitchenModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            _dataContext.Kitchen.Add(model);
            await _dataContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var kitchen = await _dataContext.Kitchen.FindAsync(id);
            return View(kitchen);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(KitchenModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            _dataContext.Kitchen.Update(model);
            await _dataContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var kitchen = await _dataContext.Kitchen.FindAsync(id);
            _dataContext.Kitchen.Remove(kitchen);
            await _dataContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task<IActionResult> KitchenScreen(int kitchenId)
        {
            var tasks = await _dataContext.OrderDetails
                .Include(od => od.Product)
                    .ThenInclude(p => p.Category)
                .Where(od => od.Product.Category.KitchenId == kitchenId
                         && od.Status != StatusProduct.Done
                         && od.Status != StatusProduct.Cancelled)
                .OrderBy(od => od.Product.Category.Priority)
                .ThenBy(od => od.CreateDate)
                .ToListAsync();
            ViewBag.Kitchen = await _dataContext.Kitchen.FindAsync(kitchenId);
            return PartialView("_KitchenBoard", tasks);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderDetailId, int status, string? returnAction)
        {
            // Lấy OrderDetail + Product + Category để biết KitchenId
            var orderDetail = await _dataContext.OrderDetails
                .Include(od => od.Product)
                    .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(od => od.Id == orderDetailId);

            if (orderDetail == null)
            {
                return NotFound();
            }

            int oldStatus = (int)orderDetail.Status;
            orderDetail.Status = (StatusProduct)status;

            // Chỉ trừ kho khi chuyển từ trạng thái KHÔNG HOÀN THÀNH -> HOÀN THÀNH (status = 2)
            bool isJustCompleted = (oldStatus != 2 && status == 2);

            // Tìm Order tương ứng (để ghi OrderId + OrderCode) — nếu cần
            var order = await _dataContext.Orders
                .FirstOrDefaultAsync(o => o.OrderCode == orderDetail.OrderCode);

            if (isJustCompleted)
            {
                var usages = await _dataContext.ProductMaterials
                    .Include(pm => pm.Material)
                    .Where(pm => pm.ProductId == orderDetail.ProductId)
                    .ToListAsync();

                foreach (var usage in usages)
                {
                    var material = usage.Material;
                    decimal usedQuantity = orderDetail.Quantity * usage.QuantityPerProduct;
                    material.CurrentQuantity -= usedQuantity;
                    _dataContext.Materials.Update(material);
                }
            }

            await _dataContext.SaveChangesAsync();
            TempData["success"] = "Cập nhật trạng thái món thành công.";

            // Lấy kitchenId (nếu có)
            int kitchenId = orderDetail?.Product?.Category?.KitchenId ?? 0;

            // Nếu caller muốn redirect về action khác, vẫn truyền selectedKitchenId để giữ tab
            if (!string.IsNullOrEmpty(returnAction))
            {
                return RedirectToAction(returnAction, new { selectedKitchenId = kitchenId });
            }

            return RedirectToAction("Index", new { selectedKitchenId = kitchenId });
        }

    }
}
