using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Helpers;
using QuanLyNhaHangDemo.Models;
using QuanLyNhaHangDemo.Models.Dtos;
using QuanLyNhaHangDemo.Models.ViewModels;
using QuanLyNhaHangDemo.Repository;
using QuanLyNhaHangDemo.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyNhaHangDemo.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin,cashier")]
    public class TableAdminController : Controller
    {
        private readonly DataContext _context;
        private readonly IOrderStateService _orderState;
        // Thêm IHubContext<YourHub> nếu bạn muốn kích hoạt Real-time cập nhật sơ đồ xuống các máy Android/Web khác

        public TableAdminController(DataContext context, IOrderStateService orderState)
        {
            _context = context;
            _orderState = orderState;
        }

        private void SetTableDimensions(TableModel table)
        {
            if (table.Capacity <= 2) { table.Width = 80; table.Height = 80; }
            else if (table.Capacity <= 4) { table.Width = 120; table.Height = 80; }
            else if (table.Capacity <= 6) { table.Width = 160; table.Height = 90; }
            else if (table.Capacity <= 10) { table.Width = 220; table.Height = 100; }
            else { table.Width = 300; table.Height = 120; }
        }

        // Hàm Helper tính toán lại tiền tổng của Order chuẩn xác, tránh sai lệch khi gộp/tách
        private void RecalculateOrderTotal(OrderModel order, List<OrderDetailsModel> details)
        {
            order.SubTotal = details.Sum(d => d.Quantity * d.UnitPrice);
            // Công thức tính GrandTotal tùy thuộc vào cấu trúc Model của bạn, ví dụ:
            // decimal vatAmount = order.SubTotal * (order.VATRate / 100);
            // decimal serviceAmount = order.SubTotal * (order.ServiceRate / 100);
            // order.GrandTotal = order.SubTotal + vatAmount + serviceAmount - order.DiscountAmount;
        }

        public async Task<IActionResult> Index()
        {
            var tables = await _context.Table
                .AsNoTracking()
                .Include(t => t.Zone)
                .OrderBy(t => t.TableName)
                .ToListAsync();

            var zones = await _context.Zones
                .AsNoTracking()
                .Where(z => z.ZoneStatus != ZoneStatus.Closed)
                .OrderBy(z => z.ZoneName)
                .ToListAsync();

            var vm = new TableFloorViewModel
            {
                Tables = tables,
                Zones = zones
            };

            return View(vm);
        }

        [HttpGet]
        [Route("Admin/TableAdmin/Create")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Zones = await _context.Zones
                .AsNoTracking()
                .Where(z => z.ZoneStatus == ZoneStatus.Active)
                .OrderBy(z => z.ZoneName)
                .ToListAsync();
            return View();
        }

        [HttpPost]
        [Route("Admin/TableAdmin/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TableModel table)
        {
            if (ModelState.IsValid)
            {
                SetTableDimensions(table);
                table.PosX = 0;
                table.PosY = 0;
                table.Status = TableStatus.Empty;

                if (!await _context.Zones.AnyAsync(z => z.ZoneId == table.ZoneId))
                {
                    ModelState.AddModelError("", "Khu vực không tồn tại");
                    ViewBag.Zones = await _context.Zones.AsNoTracking()
                        .Where(z => z.ZoneStatus == ZoneStatus.Active)
                        .OrderBy(z => z.ZoneName).ToListAsync();
                    return View(table);
                }

                _context.Table.Add(table);
                await _context.SaveChangesAsync();

                TempData["success"] = "Đã thêm bàn mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(table);
        }

        [HttpGet]
        [Route("Admin/TableAdmin/Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var table = await _context.Table.FindAsync(id);
            if (table == null) return NotFound();

            ViewBag.Zones = await _context.Zones.AsNoTracking()
                .Where(z => z.ZoneStatus == ZoneStatus.Active)
                .OrderBy(z => z.ZoneName).ToListAsync();

            return View(table);
        }

        [HttpPost]
        [Route("Admin/TableAdmin/Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TableModel model)
        {
            if (id != model.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Zones = await _context.Zones.AsNoTracking()
                    .Where(z => z.ZoneStatus == ZoneStatus.Active)
                    .OrderBy(z => z.ZoneName).ToListAsync();
                return View(model);
            }

            var table = await _context.Table.FindAsync(id);
            if (table == null) return NotFound();

            table.TableName = model.TableName;
            table.Capacity = model.Capacity;
            table.Status = model.Status;
            table.ZoneId = model.ZoneId;

            SetTableDimensions(table);
            await _context.SaveChangesAsync();
            TempData["success"] = "Cập nhật bàn thành công!";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Route("Admin/TableAdmin/UpdateLayout")]
        public async Task<IActionResult> UpdateLayout([FromBody] List<TableLayoutDto> layoutData)
        {
            if (layoutData == null || !layoutData.Any()) return Json(new { success = false });

            var ids = layoutData.Select(x => x.Id).ToList();
            var tables = await _context.Table.Where(t => ids.Contains(t.Id)).ToListAsync();

            foreach (var item in layoutData)
            {
                var table = tables.FirstOrDefault(t => t.Id == item.Id);
                if (table != null)
                {
                    table.PosX = item.PosX;
                    table.PosY = item.PosY;
                }
            }
            await _context.SaveChangesAsync();
            // TODO: Gửi tín hiệu SignalR tại đây để toàn bộ các màn hình Admin khác cập nhật vị trí mới sync theo.
            return Json(new { success = true });
        }

        [HttpGet]
        [Route("Admin/TableAdmin/FloorPlanStatus")]
        public async Task<IActionResult> FloorPlanStatus()
        {
            var readyCounts = await _orderState.GetAllTableReadyCountsAsync();
            var activeTableIds = await _context.Table
                .AsNoTracking()
                .Where(t =>
                    t.CurrentOrderId != null &&
                    t.CurrentOrder.Status != OrderModel.OrderStatus.Paid &&
                    t.CurrentOrder.Status != OrderModel.OrderStatus.Completed)
                .Select(t => t.Id)
                .Distinct()
                .ToListAsync();

            var tables = await _context.Table
                .AsNoTracking()
                .OrderBy(t => t.TableName)
                .Select(t => new
                {
                    id = t.Id,
                    status = (int)t.Status,
                    hasActiveOrder = activeTableIds.Contains(t.Id),
                    readyCount = readyCounts.ContainsKey(t.Id) ? readyCounts[t.Id] : 0
                })
                .ToListAsync();

            return Json(tables);
        }

        [HttpPost]
        [Route("Admin/TableAdmin/Checkout/{tableId}")]
        public async Task<IActionResult> Checkout(int tableId)
        {
            var (success, message) = await _orderState.CheckoutTableAsync(tableId);
            return Json(new { success, message });
        }

        [HttpGet]
        [Route("Admin/TableAdmin/GetTableDetails/{id}")]
        public async Task<IActionResult> GetTableDetails(int id)
        {
            var table = await _context.Table
                .AsNoTracking()
                .Include(t => t.CurrentOrder)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (table == null)
            {
                return NotFound();
            }

            // KHÔNG CÓ ORDER
            if (table.CurrentOrder == null)
            {
                return Json(new
                {
                    tableName = table.TableName,
                    status = (int)table.Status,
                    readyCount = 0,
                    order = (object)null
                });
            }

            var activeOrder = table.CurrentOrder;

            // ORDER KHÔNG CÒN HOẠT ĐỘNG
            if (activeOrder.Status != OrderModel.OrderStatus.Pending &&
                activeOrder.Status != OrderModel.OrderStatus.Serving)
            {
                return Json(new
                {
                    tableName = table.TableName,
                    status = (int)table.Status,
                    readyCount = 0,
                    order = (object)null
                });
            }

            // READY COUNT
            var readyCount = await _orderState
                .GetReadyItemCountAsync(id);

            // CHI TIẾT MÓN
            var orderDetails = await _context.OrderDetails
                .AsNoTracking()
                .Include(d => d.Product)
                .Include(d => d.OrderDetailModifiers)
                    .ThenInclude(odm => odm.Modifier)
                .Where(d =>
                    d.OrderId == activeOrder.Id &&
                    d.Status != StatusProduct.Cancelled)
                .ToListAsync();

            var result = new
            {
                tableName = table.TableName,

                status = (int)table.Status,

                readyCount,

                orderCode = activeOrder.OrderCode,

                orderStatus = (int)activeOrder.Status,

                orderStatusLabel =
                    OrderStatusHelper.GetOrderStatusLabel(
                        activeOrder.Status),

                isMergedMode = false,

                order = new
                {
                    orderId = activeOrder.Id,

                    orderCode = activeOrder.OrderCode,

                    orderStatus = (int)activeOrder.Status,

                    orderStatusLabel =
                        OrderStatusHelper.GetOrderStatusLabel(
                            activeOrder.Status),

                    timeIn = activeOrder.CreatedDate
                        .ToString("HH:mm"),

                    subTotal = activeOrder.SubTotal,
                    vatAmount = activeOrder.VATAmount,
                    serviceAmount = activeOrder.ServiceAmount,
                    discountAmount = activeOrder.DiscountAmount,
                    totalAmount = activeOrder.GrandTotal,

                    items = orderDetails.Select(d => new
                    {
                        id = d.Id,

                        productId = d.ProductId,

                        productName = d.Product != null
                            ? d.Product.Name
                            : "N/A",

                        quantity = d.Quantity,

                        price = d.UnitPrice,

                        note = d.Note,

                        status = (int)d.Status,

                        isFired = d.IsFired,

                        fireCount = d.FireCount,

                        statusLabel =
                            OrderStatusHelper.GetItemStatusLabel(
                                d.Status),

                        statusClass =
                            OrderStatusHelper.GetItemStatusBadgeClass(
                                d.Status),

                        modifiers = d.OrderDetailModifiers
                            .Select(m => new
                            {
                                name = m.Modifier != null
                                    ? m.Modifier.Name
                                    : "",

                                price = m.ModifierPrice
                            })
                            .ToList()
                    }).ToList()
                }
            };

            return Json(result);
        }

        [HttpPost]
        [Route("Admin/TableAdmin/Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var table = await _context.Table.FindAsync(id);
            if (table == null) return Json(new { success = false, message = "Không tìm thấy bàn cần xóa." });

            bool hasActiveOrder = await _context.Table
                .AnyAsync(t =>
                    t.Id == id &&
                    t.CurrentOrderId != null);

            if (hasActiveOrder)
                return BadRequest(new { success = false, message = "Bàn đang có khách, không thể xóa." });

            _context.Table.Remove(table);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Xóa bàn thành công." });
        }

        [HttpPost]
        [Route("Admin/TableAdmin/CreateZone")]
        public async Task<IActionResult> CreateZone(ZoneModel model)
        {
            // Kiểm tra tên khu vực
            if (string.IsNullOrWhiteSpace(model.ZoneName))
                return BadRequest(new { success = false, message = "Tên khu vực không hợp lệ" });

            // Kiểm tra kích thước hợp lệ (Tránh trường hợp nhập số âm hoặc bằng 0)
            if (model.Width <= 0 || model.Height <= 0)
                return BadRequest(new { success = false, message = "Kích thước chiều rộng và chiều cao phải lớn hơn 0." });

            if (await _context.Zones.AnyAsync(x => x.ZoneName == model.ZoneName))
                return BadRequest(new { success = false, message = "Tên khu vực đã tồn tại" });

            // EF sẽ tự động map Width và Height từ `model` vào câu lệnh Insert
            _context.Zones.Add(model);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Tạo khu vực thành công." });
        }

        [HttpPost]
        [Route("Admin/TableAdmin/UpdateZone")]
        public async Task<IActionResult> UpdateZone(ZoneModel model)
        {
            var zone = await _context.Zones.FindAsync(model.ZoneId);
            if (zone == null) return Json(new { success = false, message = "Không tìm thấy khu vực." });

            // Kiểm tra tên trùng lặp
            if (await _context.Zones.AnyAsync(x => x.ZoneId != model.ZoneId && x.ZoneName == model.ZoneName))
                return BadRequest(new { success = false, message = "Tên khu vực đã tồn tại" });

            // Kiểm tra kích thước cập nhật hợp lệ
            if (model.Width <= 0 || model.Height <= 0)
                return BadRequest(new { success = false, message = "Kích thước chiều rộng và chiều cao phải lớn hơn 0." });

            // Cập nhật các thông tin cũ
            zone.ZoneName = model.ZoneName;
            zone.ZoneStatus = model.ZoneStatus;
            zone.ZoneDescription = model.ZoneDescription; // Cập nhật thêm mô tả nếu có thay đổi

            // --- CẬP NHẬT THÊM CHIỀU RỘNG VÀ CHIỀU CAO VÀO ĐÂY ---
            zone.Width = model.Width;
            zone.Height = model.Height;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Cập nhật khu vực thành công." });
        }

        [HttpPost]
        [Route("Admin/TableAdmin/DeleteZone/{id}")]
        public async Task<IActionResult> DeleteZone(int id)
        {
            var zone = await _context.Zones.Include(z => z.Tables).FirstOrDefaultAsync(z => z.ZoneId == id);
            if (zone == null) return Json(new { success = false, message = "Không tìm thấy khu vực." });

            if (await _context.Table.AnyAsync(t => t.ZoneId == id))
                return BadRequest(new { success = false, message = "Khu vực đang có bàn, không thể xóa." });

            _context.Zones.Remove(zone);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Xóa khu vực thành công." });
        }
        
    }
}