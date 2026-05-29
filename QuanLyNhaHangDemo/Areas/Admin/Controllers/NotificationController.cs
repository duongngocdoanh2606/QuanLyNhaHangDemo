using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Models;
using QuanLyNhaHangDemo.Repository;

namespace QuanLyNhaHangDemo.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/Notification")]
    public class NotificationController : Controller
    {
        private readonly DataContext _context;

        public NotificationController(DataContext context)
        {
            _context = context;
        }

        [HttpGet("Unread")]
        public async Task<IActionResult> Unread()
        {
            var fifteenMinutesAgo = DateTime.Now.AddMinutes(-15);

            // Lấy toàn bộ bàn đang có order hoạt động
            var activeTables = await _context.Table
                .Include(t => t.CurrentOrder)
                .ThenInclude(o => o.OrderDetails)
                .Where(t =>
                    t.CurrentOrderId != null &&
                    (t.CurrentOrder.Status == OrderModel.OrderStatus.Pending ||
                     t.CurrentOrder.Status == OrderModel.OrderStatus.Serving))
                .ToListAsync();

            foreach (var table in activeTables)
            {
                var order = table.CurrentOrder;

                if (order == null)
                    continue;

                // Lấy các món đã Served
                var servedItems = order.OrderDetails
                    .Where(x => x.Status == StatusProduct.Served)
                    .ToList();

                DateTime baseTime;

                if (servedItems.Any())
                {
                    // Reset thời gian theo món Served gần nhất
                    baseTime = servedItems.Max(x =>
                        x.UpdatedAt ?? x.CreateDate);
                }
                else
                {
                    // Chưa served món nào -> tính từ lúc tạo order
                    baseTime = order.CreatedDate;
                }

                // Kiểm tra còn món chưa xong không
                bool hasRemainingItems = order.OrderDetails.Any(x =>
                    x.Status != StatusProduct.Served &&
                    x.Status != StatusProduct.Cancelled);

                if (!hasRemainingItems)
                    continue;

                // Quá 15 phút chưa phục vụ thêm món
                if (baseTime <= fifteenMinutesAgo)
                {
                    string msg =
                        $"Đơn hàng {order.OrderCode} đã quá 15 phút chưa có thêm món mới nào được phục vụ!";

                    bool exists = await _context.AdminNotifications
                        .AnyAsync(n =>
                            n.TableId == table.Id &&
                            n.Message.Contains(order.OrderCode) &&
                            !n.IsRead);

                    if (!exists)
                    {
                        await _context.AdminNotifications.AddAsync(
                            new AdminNotificationModel
                            {
                                TableId = table.Id,
                                OrderId = order.Id,
                                Message = msg,
                                ProductName = "CẢNH BÁO TRỄ MÓN",
                                CreatedAt = DateTime.Now,
                                IsRead = false
                            });
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Trả về danh sách thông báo chưa đọc
            var list = await _context.AdminNotifications
                .Where(n => !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Take(20)
                .Select(n => new
                {
                    n.Id,
                    n.TableId,
                    n.Message,
                    n.ProductName,
                    n.CreatedAt
                })
                .ToListAsync();

            int count = await _context.AdminNotifications
                .CountAsync(n => !n.IsRead);

            return Ok(new
            {
                count,
                items = list
            });
        }

        [HttpPost("MarkRead/{id}")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var n = await _context.AdminNotifications.FindAsync(id);
            if (n == null) return NotFound();
            n.IsRead = true;
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost("MarkAllRead")]
        public async Task<IActionResult> MarkAllRead()
        {
            var unread = await _context.AdminNotifications.Where(n => !n.IsRead).ToListAsync();
            foreach (var n in unread)
                n.IsRead = true;
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}
