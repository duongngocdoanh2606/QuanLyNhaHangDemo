using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Models;
using QuanLyNhaHangDemo.Repository;
using QuanLyNhaHangDemo.Services;

namespace QuanLyNhaHangDemo.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class OrderController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly IOrderStateService _orderState;

        public OrderController(
            DataContext context,
            IOrderStateService orderState)
        {
            _dataContext = context;
            _orderState = orderState;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var orders = await _dataContext.Table
                .Include(t => t.CurrentOrder)
                .ThenInclude(o => o.OrderDetails)
                .Where(t =>
                    t.CurrentOrderId != null &&
                    t.CurrentOrder.Status != OrderModel.OrderStatus.Paid &&
                    t.CurrentOrder.Status != OrderModel.OrderStatus.Cancelled)
                .Select(t => t.CurrentOrder)
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            var fifteenMinutesAgo = DateTime.Now.AddMinutes(-15);

            var delayedOrderCodes = new HashSet<string>();

            foreach (var order in orders)
            {
                var servedItems = order.OrderDetails
                    .Where(x => x.Status == StatusProduct.Served)
                    .ToList();

                DateTime baseCheckTime;

                if (servedItems.Any())
                {
                    baseCheckTime = servedItems.Max(x =>
                        x.UpdatedAt ?? x.CreateDate);
                }
                else
                {
                    baseCheckTime = order.CreatedDate;
                }

                bool hasRemainingItems = order.OrderDetails.Any(x =>
                    x.Status != StatusProduct.Served &&
                    x.Status != StatusProduct.Cancelled);

                if (baseCheckTime <= fifteenMinutesAgo &&
                    hasRemainingItems)
                {
                    delayedOrderCodes.Add(order.OrderCode);
                }
            }

            ViewBag.DelayedOrderCodes = delayedOrderCodes;

            return View(orders);
        }

        [HttpGet]
        [Route("ViewOrder")]
        public async Task<IActionResult> ViewOrder(string ordercode)
        {
            var order = await _dataContext.Orders
                .FirstOrDefaultAsync(o => o.OrderCode == ordercode);

            if (order == null)
                return NotFound();

            var detailsOrder = await _dataContext.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order)
                .Include(od => od.OrderDetailModifiers)
                    .ThenInclude(odm => odm.Modifier)
                .Where(od =>
                    od.OrderId == order.Id &&
                    od.Status != StatusProduct.Cancelled)
                .ToListAsync();

            ViewBag.Status = order.Status;

            ViewBag.OrderStatusLabel =
                Helpers.OrderStatusHelper
                    .GetOrderStatusLabel(order.Status);

            ViewBag.OrderStatusClass =
                Helpers.OrderStatusHelper
                    .GetOrderStatusBadgeClass(order.Status);

            ViewBag.CanModifyItems =
                order.Status != OrderModel.OrderStatus.Paid &&
                order.Status != OrderModel.OrderStatus.Cancelled;

            return View(detailsOrder);
        }

        [HttpPost]
        [Route("CancelItem")]
        public async Task<IActionResult> CancelItem(int orderDetailId)
        {
            var detail = await _dataContext.OrderDetails
                .FindAsync(orderDetailId);

            if (detail == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Không tìm thấy món ăn cần hủy."
                });
            }

            if (detail.Status == StatusProduct.Done ||
                detail.Status == StatusProduct.Served)
            {
                return Json(new
                {
                    success = false,
                    message = "Món đã hoàn thành, không thể hủy."
                });
            }

            var oldStatus = detail.Status;

            detail.Status = StatusProduct.Cancelled;
            detail.UpdatedAt = DateTime.Now;

            await _dataContext.SaveChangesAsync();

            await _orderState.SyncAfterOrderDetailStatusChangeAsync(
                orderDetailId,
                oldStatus,
                detail.Status);

            return Json(new
            {
                success = true,
                message = "Đã hủy món thành công."
            });
        }

        [HttpPost]
        [Route("FireItem")]
        public async Task<IActionResult> FireItem(
            int orderDetailId,
            bool remake = false)
        {
            var (success, message) =
                await _orderState.FireOrderDetailAsync(
                    orderDetailId,
                    remake);

            return Json(new { success, message });
        }

        [HttpPost]
        [Route("Admin/TableAdmin/ServeItem/{orderDetailId}")]
        public async Task<IActionResult> ServeItem(int orderDetailId)
        {
            var detail = await _dataContext.OrderDetails
                .Include(x => x.Order)
                .FirstOrDefaultAsync(x => x.Id == orderDetailId);

            if (detail == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Không tìm thấy món."
                });
            }

            if (detail.Status == StatusProduct.Served)
            {
                return Json(new
                {
                    success = false,
                    message = "Món đã phục vụ."
                });
            }

            if (detail.Status != StatusProduct.Done)
            {
                return Json(new
                {
                    success = false,
                    message = "Món chưa hoàn thành."
                });
            }

            var oldStatus = detail.Status;

            detail.Status = StatusProduct.Served;
            detail.UpdatedAt = DateTime.Now;

            var product = await _dataContext.Products
                .FindAsync(detail.ProductId);

            if (product != null)
            {
                product.Sold += detail.Quantity;
            }

            await _dataContext.SaveChangesAsync();

            await _orderState.SyncAfterOrderDetailStatusChangeAsync(
                orderDetailId,
                oldStatus,
                detail.Status);

            return Json(new
            {
                success = true,
                message = "Đã phục vụ món."
            });
        }

        [HttpPost]
        [Route("UpdateOrder")]
        public async Task<IActionResult> UpdateOrder(
            string ordercode,
            int status)
        {
            if (string.IsNullOrEmpty(ordercode))
            {
                return Json(new
                {
                    success = false,
                    message = "Mã đơn rỗng."
                });
            }

            var order = await _dataContext.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o =>
                    o.OrderCode == ordercode);

            if (order == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Không tìm thấy đơn."
                });
            }

            var oldStatus = order.Status;

            order.Status =
                (OrderModel.OrderStatus)status;

            try
            {
                await _dataContext.SaveChangesAsync();

                await _orderState.SyncAfterOrderStatusChangeAsync(
                    order.Id,
                    oldStatus,
                    order.Status);

                return Json(new
                {
                    success = true
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CompletedOrders(
            DateTime? from,
            DateTime? to)
        {
            var query = _dataContext.Orders
                .Include(o => o.Coupon)
                .Where(o =>
                    o.Status == OrderModel.OrderStatus.Paid)
                .AsQueryable();

            if (from.HasValue)
            {
                query = query.Where(o =>
                    o.CreatedDate.Date >= from.Value.Date);
            }

            if (to.HasValue)
            {
                query = query.Where(o =>
                    o.CreatedDate.Date <= to.Value.Date);
            }

            var orders = await query
                .OrderByDescending(o => o.CreatedDate)
                .ToListAsync();

            var list = orders.Select(o => new CompletedOrderViewModel
            {
                OrderCode = o.OrderCode ?? "",
                UserName = string.IsNullOrEmpty(o.GuestName) ? "Khách vãng lai" : o.GuestName,
                CreatedDate = o.CreatedDate,
                CouponCode = o.Coupon?.Code ?? "",
                CouponDiscount = o.DiscountAmount,
                VATAmount = o.VATAmount,
                ServiceAmount = o.ServiceAmount,
                OrderRevenue = o.SubTotal,
                TotalWithCoupon = o.GrandTotal
            }).ToList();

            ViewBag.TotalRevenue = list.Sum(x => x.OrderRevenue);
            ViewBag.TotalRevenueWithCoupon = list.Sum(x => x.TotalWithCoupon);

            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Invoice(string ordercode)
        {
            if (string.IsNullOrEmpty(ordercode))
                return NotFound();

            var order = await _dataContext.Orders
                .FirstOrDefaultAsync(o =>
                    o.OrderCode == ordercode);

            if (order == null)
                return NotFound();

            var details = await _dataContext.OrderDetails
                .Include(d => d.Product)
                .Where(d =>
                    d.OrderId == order.Id &&
                    d.Status != StatusProduct.Cancelled)
                .ToListAsync();

            decimal total = details.Sum(x =>
                x.Quantity * x.UnitPrice);

            var table = await _dataContext.Table
                .FirstOrDefaultAsync(t =>
                    t.CurrentOrderId == order.Id);

            ViewBag.Order = order;
            ViewBag.Total = total;
            ViewBag.Buyer = order.GuestName;
            ViewBag.OrderCode = ordercode;

            ViewBag.TableInfo =
                table != null
                    ? $"Bàn số: {table.TableName}"
                    : "Không xác định";

            return View(details);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string ordercode)
        {
            if (string.IsNullOrEmpty(ordercode))
                return NotFound();

            var order = await _dataContext.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o =>
                    o.OrderCode == ordercode);

            if (order == null)
                return NotFound();

            try
            {
                var table = await _dataContext.Table
                    .FirstOrDefaultAsync(t =>
                        t.CurrentOrderId == order.Id);

                if (table != null)
                {
                    table.CurrentOrderId = null;
                    table.Status = TableStatus.Empty;
                }

                _dataContext.OrderDetails
                    .RemoveRange(order.OrderDetails);

                _dataContext.Orders.Remove(order);

                await _dataContext.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return StatusCode(
                    500,
                    "Lỗi khi xóa đơn hàng.");
            }
        }
    }
}