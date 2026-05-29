using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Repository;
using QuanLyNhaHangDemo.Models;
using QuanLyNhaHangDemo.Models.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyNhaHangDemo.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class TablesApiController : ControllerBase
    {
        private readonly DataContext _db;

        public TablesApiController(DataContext db)
        {
            _db = db;
        }

        internal static string StatusStr(TableStatus s) => s switch
        {
            TableStatus.Empty => "available",
            TableStatus.Serving => "occupied",
            TableStatus.Reserved => "reserved",
            _ => "available"
        };

        private static TableStatus ParseStatus(string s) => s?.ToLower() switch
        {
            "available" => TableStatus.Empty,
            "occupied" => TableStatus.Serving,
            "reserved" => TableStatus.Reserved,
            "dirty" => TableStatus.Empty,
            _ => TableStatus.Empty
        };

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TableMapDto>>> GetAll()
        {
            var tables = await _db.Table
                .AsNoTracking()
                .Include(t => t.Zone)
                .Select(t => new TableMapDto
                {
                    Id = t.Id,
                    Name = t.TableName,
                    Capacity = t.Capacity,
                    Status = StatusStr(t.Status),

                    // đổi chỗ này
                    Zone = t.Zone != null
                        ? t.Zone.ZoneName
                        : ""
                })
                .ToListAsync();

            return Ok(tables);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TableDetailDto>> GetById(int id)
        {
            // 1. Lấy thông tin bàn hiện tại + Đơn hàng đang gắn với bàn này
            var table = await _db.Table
                .Include(t => t.Zone)
                .Include(t => t.CurrentOrder)
                    .ThenInclude(o => o.OrderDetails.Where(od => od.Status != StatusProduct.Cancelled))
                    .ThenInclude(od => od.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (table == null)
            {
                return NotFound(new { message = "Không tìm thấy bàn." });
            }

            var activeOrder = table.CurrentOrder;
            bool isMergedTable = false;
            string mergedFromTableName = "";

            // 2. Logic kiểm tra bàn gộp (Vì Order không còn TableId)
            // Ta dựa vào việc: Nếu bàn có ActiveOrder, và Note của Order này chứa thông tin gộp đơn
            if (activeOrder != null && !string.IsNullOrEmpty(activeOrder.Note))
            {
                // Giả sử khi gộp bàn, bạn ghi Note ở đơn của bàn phụ là: "MERGED_TO_ORDER_【IdĐơnChính】"
                if (activeOrder.Note.StartsWith("MERGED_TO_ORDER_"))
                {
                    isMergedTable = true;

                    var targetOrderString = activeOrder.Note.Replace("MERGED_TO_ORDER_", "");
                    if (int.TryParse(targetOrderString, out int targetOrderId))
                    {
                        // Tìm xem bàn nào đang là bàn chính giữ cái targetOrderId này
                        mergedFromTableName = await _db.Table
                            .AsNoTracking()
                            .Where(t => t.CurrentOrderId == targetOrderId && t.Id != id)
                            .Select(t => t.TableName)
                            .FirstOrDefaultAsync() ?? "Bàn chính";
                    }
                }
            }

            // 3. Nếu thực sự không có order nào đang chạy tại bàn này
            if (activeOrder == null)
            {
                return Ok(new TableDetailDto
                {
                    Id = table.Id,
                    Name = table.TableName,
                    Capacity = table.Capacity,
                    Status = StatusStr(table.Status),
                    Zone = table.Zone?.ZoneName ?? "",
                    ActiveOrder = null
                });
            }

            // 4. Map danh sách món ăn từ dữ liệu đã Include ở Bước 1
            var itemsDto = activeOrder.OrderDetails.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                Name = i.Product?.Name ?? "N/A",
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                SubTotal = i.Quantity * i.UnitPrice,
                Status = i.Status.ToString().ToLower()
            }).ToList();

            // 5. Build response hoàn chỉnh
            var dto = new TableDetailDto
            {
                Id = table.Id,
                // Nếu là bàn phụ được gộp → Hiển thị: "Bàn 2 (Gộp với Bàn 1)"
                Name = isMergedTable
                    ? $"{table.TableName} (Gộp với {mergedFromTableName})"
                    : table.TableName,

                Capacity = table.Capacity,
                Status = StatusStr(table.Status),
                Zone = table.Zone?.ZoneName ?? "",

                ActiveOrder = new OrderSummaryDto
                {
                    OrderCode = activeOrder.OrderCode,
                    GuestName = activeOrder.GuestName,
                    CheckInTime = activeOrder.CreatedDate.ToString("HH:mm"),
                    TotalAmount = activeOrder.GrandTotal,
                    Items = itemsDto
                }
            };

            return Ok(dto);
        }

        [HttpPatch("{id:int}/status")]
        public async Task<ActionResult> UpdateStatus(int id, [FromBody] UpdateTableStatusRequest req)
        {
            var table = await _db.Table.FindAsync(id);
            if (table is null) return NotFound(new { message = "Không tìm thấy bàn." });

            table.Status = ParseStatus(req.Status);
            await _db.SaveChangesAsync();

            return Ok(new { tableId = id, status = StatusStr(table.Status) });
        }

        [HttpGet("summary")]
        public async Task<ActionResult> GetSummary()
        {
            var summaryDb = await _db.Table
                .GroupBy(t => t.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            // Nhóm và map danh sách trạng thái dạng chuỗi trả về cho Client
            var summary = summaryDb.Select(x => new { Status = StatusStr(x.Status), Count = x.Count });

            return Ok(summary);
        }

        [HttpPost("merge")]
        public async Task<ActionResult> MergeTable([FromBody] MergeTableRequest req)
        {
            var sourceTable = await _db.Table.Include(t => t.CurrentOrder).FirstOrDefaultAsync(t => t.Id == req.SourceTableId);
            var targetTable = await _db.Table.Include(t => t.CurrentOrder).FirstOrDefaultAsync(t => t.Id == req.TargetTableId);

            if (sourceTable == null || targetTable == null)
                return NotFound(new { message = "Không tìm thấy bàn nguồn hoặc bàn đích." });

            if (sourceTable.CurrentOrderId == null)
                return BadRequest(new { message = "Bàn nguồn đang trống, không có đơn để gộp." });

            if (targetTable.CurrentOrderId == null)
                return BadRequest(new { message = "Bàn đích đang trống, không thể gộp vào bàn đích. Vui lòng chọn chức năng Chuyển Bàn." });

            if (sourceTable.Id == targetTable.Id)
                return BadRequest(new { message = "Không thể gộp cùng một bàn." });

            var sourceOrder = sourceTable.CurrentOrder;
            var targetOrder = targetTable.CurrentOrder;

            // Chuyển toàn bộ món từ bàn nguồn sang bàn đích và gộp các món giống nhau
            var targetOrderDetails = await _db.OrderDetails
                .Include(od => od.OrderDetailModifiers)
                .Where(od => od.OrderId == targetOrder.Id)
                .ToListAsync();

            var sourceOrderDetails = await _db.OrderDetails
                .Include(od => od.OrderDetailModifiers)
                .Where(od => od.OrderId == sourceOrder.Id)
                .ToListAsync();

            foreach (var sourceDetail in sourceOrderDetails)
            {
                // Tìm xem ở đích có món nào GIỐNG Y HỆT không (cùng ProductId, Status, Note, Giá và Modifier)
                var existingTargetDetail = targetOrderDetails.FirstOrDefault(td =>
                    td.ProductId == sourceDetail.ProductId &&
                    td.Status == sourceDetail.Status &&
                    td.UnitPrice == sourceDetail.UnitPrice &&
                    (td.Note ?? "") == (sourceDetail.Note ?? "") &&
                    td.OrderDetailModifiers.Count == sourceDetail.OrderDetailModifiers.Count &&
                    td.OrderDetailModifiers.All(m => sourceDetail.OrderDetailModifiers.Any(sm => sm.ModifierId == m.ModifierId))
                );

                if (existingTargetDetail != null)
                {
                    // Cộng dồn số lượng
                    existingTargetDetail.Quantity += sourceDetail.Quantity;
                    // Xóa chi tiết cũ vì đã được gộp
                    _db.OrderDetails.Remove(sourceDetail);
                }
                else
                {
                    // Chuyển món này sang đơn đích
                    sourceDetail.OrderId = targetOrder.Id;
                    targetOrderDetails.Add(sourceDetail);
                }
            }

            // Tính lại tổng tiền cho đơn hàng đích
            targetOrder.SubTotal += sourceOrder.SubTotal;
            if (targetOrder.CouponId.HasValue)
            {
                var coupon = await _db.Coupons.FindAsync(targetOrder.CouponId.Value);
                if (coupon != null)
                {
                    targetOrder.DiscountAmount = coupon.DiscountAmount > targetOrder.SubTotal ? targetOrder.SubTotal : coupon.DiscountAmount;
                }
            }

            // Đổi con trỏ bàn nguồn sang bàn đích, thêm ghi chú gộp bàn
            targetOrder.Note = string.IsNullOrEmpty(targetOrder.Note) 
                ? $"Đã gộp đơn từ {sourceTable.TableName}" 
                : targetOrder.Note + $", Đã gộp đơn từ {sourceTable.TableName}";

            // Huỷ hoặc xoá đơn cũ
            sourceOrder.Status = OrderModel.OrderStatus.Cancelled;
            sourceOrder.Note = $"Đã chuyển gộp sang đơn của {targetTable.TableName}";

            sourceTable.CurrentOrderId = targetOrder.Id;
            sourceTable.Status = TableStatus.Serving;

            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Gộp bàn thành công." });
        }

        [HttpPost("transfer")]
        public async Task<ActionResult> TransferTable([FromBody] TransferTableRequest req)
        {
            var sourceTable = await _db.Table.FindAsync(req.SourceTableId);
            var targetTable = await _db.Table.FindAsync(req.TargetTableId);

            if (sourceTable == null || targetTable == null)
                return NotFound(new { message = "Không tìm thấy bàn nguồn hoặc bàn đích." });

            if (sourceTable.CurrentOrderId == null)
                return BadRequest(new { message = "Bàn nguồn không có đơn hàng để chuyển." });

            if (targetTable.CurrentOrderId != null)
                return BadRequest(new { message = "Bàn đích đã có đơn hàng. Vui lòng dùng chức năng Gộp bàn." });

            if (sourceTable.Id == targetTable.Id)
                return BadRequest(new { message = "Không thể chuyển sang cùng một bàn." });

            int activeOrderId = sourceTable.CurrentOrderId.Value;
            targetTable.CurrentOrderId = activeOrderId;
            targetTable.Status = TableStatus.Serving;

            // Lấy tất cả các bàn đang chung đơn (nếu có gộp bàn)
            var mergedTables = await _db.Table.Where(t => t.CurrentOrderId == activeOrderId).ToListAsync();
            foreach (var table in mergedTables)
            {
                if (table.Id != targetTable.Id)
                {
                    table.CurrentOrderId = null;
                    table.Status = TableStatus.Empty;
                }
            }

            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Chuyển bàn thành công." });
        }
    }

    public class MergeTableRequest
    {
        public int SourceTableId { get; set; }
        public int TargetTableId { get; set; }
    }

    public class TransferTableRequest
    {
        public int SourceTableId { get; set; }
        public int TargetTableId { get; set; }
    }
}