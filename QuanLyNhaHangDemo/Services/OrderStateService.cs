using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Hubs;
using QuanLyNhaHangDemo.Models;
using QuanLyNhaHangDemo.Repository;
using System.Collections.Concurrent;

namespace QuanLyNhaHangDemo.Services
{
    public class OrderStateService : IOrderStateService
    {
        private readonly DataContext _db;
        private readonly IHubContext<OrderHub> _hub;
        private static readonly ConcurrentDictionary<int, SemaphoreSlim> _orderLocks = new();

        public OrderStateService(
            DataContext db,
            IHubContext<OrderHub> hub)
        {
            _db = db;
            _hub = hub;
        }

        public async Task SyncAfterOrderDetailStatusChangeAsync(
            int orderDetailId,
            StatusProduct oldStatus,
            StatusProduct newStatus)
        {
            var detail = await _db.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order)
                .FirstOrDefaultAsync(od => od.Id == orderDetailId);

            if (detail?.Order == null)
                return;

            var order = detail.Order;

            await SyncOrderStatusFromDetailsAsync(order);

            var tableIds = await _db.Table
                .Where(t => t.CurrentOrderId == order.Id)
                .Select(t => t.Id)
                .ToListAsync();

            foreach (var tableId in tableIds)
            {
                await SyncTableStatusAsync(tableId);
            }

            if (oldStatus != StatusProduct.Done &&
                newStatus == StatusProduct.Done)
            {
                await NotifyDishReadyAsync(detail);
            }

            // SỬA LỖI: Phải lưu DB trước khi chạy logic kích hoạt nhóm tiếp theo
            // để câu lệnh query trong TriggerNextSequenceAsync nhìn thấy đúng trạng thái mới phục vụ (Served).
            await _db.SaveChangesAsync();

            if (oldStatus != StatusProduct.Served &&
                newStatus == StatusProduct.Served)
            {
                await TriggerNextSequenceAsync(order.Id, detail.Id);
            }

            await BroadcastFloorPlanUpdateAsync();
        }

        public async Task SyncAfterOrderStatusChangeAsync(
            int orderId,
            OrderModel.OrderStatus oldStatus,
            OrderModel.OrderStatus newStatus)
        {
            var order = await _db.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return;

            string tableInfo = "";
            if (newStatus == OrderModel.OrderStatus.Paid ||
                newStatus == OrderModel.OrderStatus.Cancelled)
            {
                var tables = await _db.Table
                    .Where(t => t.CurrentOrderId == order.Id)
                    .ToListAsync();

                if (tables.Any())
                {
                    tableInfo = string.Join(", ", tables.Select(t => $"Bàn {t.TableName}"));
                }

                foreach (var table in tables)
                {
                    table.Status = TableStatus.Empty;
                    table.CurrentOrderId = null;

                    await MarkTableNotificationsReadAsync(table.Id);
                }
            }

            if (newStatus == OrderModel.OrderStatus.Paid)
            {
                await NotifyOrderPaidAsync(order, tableInfo);
            }

            await _db.SaveChangesAsync();

            await BroadcastFloorPlanUpdateAsync();
        }

        public async Task<int> GetReadyItemCountAsync(int tableId)
        {
            var orderId = await GetActiveOrderIdAsync(tableId);

            if (!orderId.HasValue)
                return 0;

            return await _db.OrderDetails
                .CountAsync(od =>
                    od.OrderId == orderId.Value &&
                    od.Status == StatusProduct.Done);
        }

        public async Task MarkTableItemsServedAsync(int tableId)
        {
            var orderId = await GetActiveOrderIdAsync(tableId);

            if (!orderId.HasValue)
                return;

            var readyItems = await _db.OrderDetails
                .Where(od =>
                    od.OrderId == orderId.Value &&
                    od.Status == StatusProduct.Done)
                .ToListAsync();

            if (!readyItems.Any())
                return;

            foreach (var item in readyItems)
            {
                item.Status = StatusProduct.Served;
            }

            await MarkTableNotificationsReadAsync(tableId);

            // Lưu thay đổi trạng thái Served của toàn bộ món lên DB trước
            await _db.SaveChangesAsync();

            await BroadcastFloorPlanUpdateAsync();

            // SỬA LỖI: Truyền null vào để hàm tự động rà soát chuỗi cuốn chiếu cho các nhóm sau
            await TriggerNextSequenceAsync(orderId.Value, null);
        }

        private async Task SyncOrderStatusFromDetailsAsync(OrderModel order)
        {
            if (order.Status == OrderModel.OrderStatus.Paid ||
                order.Status == OrderModel.OrderStatus.Cancelled ||
                order.Status == OrderModel.OrderStatus.Completed)
            {
                return;
            }

            var details = await _db.OrderDetails
                .Where(od =>
                    od.OrderId == order.Id &&
                    od.Status != StatusProduct.Cancelled)
                .Select(od => od.Status)
                .ToListAsync();

            if (!details.Any())
                return;

            bool hasActiveKitchen = details.Any(s =>
                s == StatusProduct.Pending ||
                s == StatusProduct.Cooking ||
                s == StatusProduct.PreparingIngredient);

            bool hasReady = details.Any(s =>
                s == StatusProduct.Done);

            if (hasActiveKitchen || hasReady)
            {
                if (order.Status == OrderModel.OrderStatus.Pending)
                {
                    order.Status = OrderModel.OrderStatus.Serving;
                }
            }
        }

        private async Task SyncTableStatusAsync(int tableId)
        {
            var table = await _db.Table
                .FirstOrDefaultAsync(t => t.Id == tableId);

            if (table == null)
                return;

            table.Status = table.CurrentOrderId != null ? TableStatus.Serving : TableStatus.Empty;
        }

        private async Task NotifyDishReadyAsync(OrderDetailsModel detail)
        {
            var tables = await _db.Table
                .Include(t => t.Zone)
                .Where(t => t.CurrentOrderId == detail.OrderId)
                .ToListAsync();

            if (!tables.Any())
                return;

            foreach (var table in tables)
            {
                string tableName = table.TableName;
                string zoneName = table.Zone?.ZoneName ?? "Khu vực chung";
                string productName = detail.Product?.Name ?? "Món";
                string notifMessage = $"{zoneName} - Bàn {tableName}: {productName} sẵn sàng phục vụ";

                _db.AdminNotifications.Add(
                    new AdminNotificationModel
                    {
                        TableId = table.Id,
                        OrderId = detail.OrderId,
                        OrderDetailId = detail.Id,
                        ProductName = productName,
                        Message = notifMessage,
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });

                var payload = new
                {
                    tableId = table.Id,
                    tableName,
                    orderDetailId = detail.Id,
                    productName,
                    message = notifMessage,
                    readyCount = await GetReadyItemCountAsync(table.Id)
                };

                await _hub.Clients
                    .Group(OrderHub.AdminGroup)
                    .SendAsync("DishReady", payload);

                await _hub.Clients
                    .Group(OrderHub.FloorPlanGroup)
                    .SendAsync("TableReadyUpdate",
                        new
                        {
                            tableId = table.Id,
                            readyCount = payload.readyCount
                        });
            }
        }

        public async Task NotifyNewOrderAsync(int orderId, bool isDraft = false)
        {
            var order = await _db.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return;

            var tables = await _db.Table
                .Include(t => t.Zone)
                .Where(t => t.CurrentOrderId == orderId)
                .ToListAsync();

            string tableInfo = tables.Any()
                ? string.Join(", ", tables.Select(t => $"Bàn {t.TableName}"))
                : "Chưa xác định";

            string statusText = isDraft ? "ĐƠN CHỜ XÁC NHẬN" : "Đơn mới";
            string message = $"{statusText}: {order.OrderCode} — {tableInfo}";

            await _hub.Clients
                .Group(OrderHub.AdminGroup)
                .SendAsync("NewOrderCreated", new
                {
                    orderId = order.Id,
                    orderCode = order.OrderCode,
                    tableInfo,
                    message,
                    createdAt = order.CreatedDate.ToString("HH:mm"),
                    isDraft
                });
        }

        private async Task NotifyOrderPaidAsync(OrderModel order, string tableInfo)
        {
            string message = string.IsNullOrEmpty(tableInfo)
                ? $"Đơn {order.OrderCode} đã thanh toán thành công."
                : $"Đơn {order.OrderCode} ({tableInfo}) đã thanh toán thành công.";

            await _hub.Clients
                .Group(OrderHub.AdminGroup)
                .SendAsync("OrderPaid", new
                {
                    orderId = order.Id,
                    orderCode = order.OrderCode,
                    tableInfo,
                    message
                });
        }

        private async Task NotifyDishRemakeAsync(OrderDetailsModel detail)
        {
            var tables = await _db.Table
                .Where(t => t.CurrentOrderId == detail.OrderId)
                .ToListAsync();

            string tableInfo = tables.Any() ? string.Join(", ", tables.Select(t => $"Bàn {t.TableName}")) : "";
            string productName = detail.Product?.Name ?? "Món";
            string message = $"Yêu cầu làm lại món: {productName} — {tableInfo}";

            await _hub.Clients
                .Group(OrderHub.AdminGroup)
                .SendAsync("DishRemade", new
                {
                    orderDetailId = detail.Id,
                    productName,
                    tableInfo,
                    message
                });
        }

        private async Task BroadcastFloorPlanUpdateAsync()
        {
            var statuses = await GetAllTableReadyCountsAsync();
            await _hub.Clients
                .Group(OrderHub.FloorPlanGroup)
                .SendAsync("FloorPlanRefresh", statuses);
        }

        private async Task BroadcastKitchenRefreshAsync()
        {
            await _hub.Clients
                .Group(OrderHub.AdminGroup)
                .SendAsync("KitchenRefresh");
        }

        public async Task<(bool Success, string Message)> FireOrderDetailAsync(int orderDetailId, bool isRemake)
        {
            var detail = await _db.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order)
                .FirstOrDefaultAsync(od => od.Id == orderDetailId);

            if (detail == null)
                return (false, "Không tìm thấy món.");

            if (detail.Status == StatusProduct.Cancelled)
                return (false, "Món đã hủy.");

            if (detail.Order.Status == OrderModel.OrderStatus.Paid ||
                detail.Order.Status == OrderModel.OrderStatus.Cancelled)
            {
                return (false, "Đơn đã kết thúc.");
            }

            StatusProduct oldStatus = detail.Status;

            if (isRemake)
            {
                if (detail.Status != StatusProduct.Done &&
                    detail.Status != StatusProduct.Served)
                {
                    return (false, "Chỉ làm lại món đã xong.");
                }

                detail.FireCount++;
                detail.Status = StatusProduct.Pending;
            }
            else if (detail.Status == StatusProduct.Done ||
                     detail.Status == StatusProduct.Served)
            {
                return (false, "Món đã xong.");
            }

            detail.IsFired = true;
            detail.FiredAt = DateTime.Now;
            detail.IsManuallyFired = true;

            if (!isRemake && detail.Status == StatusProduct.Pending)
            {
                detail.Status = StatusProduct.Cooking;
            }

            StatusProduct newStatus = detail.Status;

            await _db.SaveChangesAsync();

            await SyncAfterOrderDetailStatusChangeAsync(orderDetailId, oldStatus, newStatus);

            await BroadcastKitchenRefreshAsync();

            await _hub.Clients
                .Group(OrderHub.AdminGroup)
                .SendAsync("DishFired",
                    new
                    {
                        orderDetailId = detail.Id,
                        productName = detail.Product?.Name,
                        isRemake,
                        message = isRemake ? $"Làm lại: {detail.Product?.Name}" : $"Fire ưu tiên: {detail.Product?.Name}"
                    });

            if (isRemake)
            {
                await NotifyDishRemakeAsync(detail);
            }

            return (true, isRemake ? "Đã gửi yêu cầu làm lại món." : "Đã fire món.");
        }

        public async Task<(bool Success, string Message)> CheckoutTableAsync(int tableId)
        {
            var orderId = await GetActiveOrderIdAsync(tableId);

            if (!orderId.HasValue)
            {
                return (false, "Không có đơn đang phục vụ.");
            }

            var order = await _db.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId.Value);

            if (order == null)
            {
                return (false, "Không tìm thấy đơn.");
            }

            var oldStatus = order.Status;
            order.Status = OrderModel.OrderStatus.Paid;

            await _db.SaveChangesAsync();

            await SyncAfterOrderStatusChangeAsync(order.Id, oldStatus, OrderModel.OrderStatus.Paid);

            return (true, "Đã thanh toán.");
        }

        public async Task<Dictionary<int, int>> GetAllTableReadyCountsAsync()
        {
            var tables = await _db.Table
                .Where(t => t.CurrentOrderId != null)
                .Select(t => new
                {
                    TableId = t.Id,
                    OrderId = t.CurrentOrderId.Value
                })
                .ToListAsync();

            if (!tables.Any())
            {
                return new Dictionary<int, int>();
            }

            var orderIds = tables.Select(t => t.OrderId).Distinct().ToList();

            var readyByOrder = await _db.OrderDetails
                .Where(od => orderIds.Contains(od.OrderId) && od.Status == StatusProduct.Done)
                .GroupBy(od => od.OrderId)
                .Select(g => new
                {
                    OrderId = g.Key,
                    Count = g.Count()
                })
                .ToDictionaryAsync(x => x.OrderId, x => x.Count);

            var result = new Dictionary<int, int>();
            foreach (var table in tables)
            {
                int count = readyByOrder.GetValueOrDefault(table.OrderId, 0);
                if (count > 0)
                {
                    result[table.TableId] = count;
                }
            }

            return result;
        }

        private async Task<int?> GetActiveOrderIdAsync(int tableId)
        {
            return await _db.Table
                .Where(t => t.Id == tableId && t.CurrentOrderId != null)
                .Select(t => t.CurrentOrderId)
                .FirstOrDefaultAsync();
        }

        private async Task<bool> HasActiveOrderOnTableAsync(int tableId, int? excludeOrderId)
        {
            var table = await _db.Table.FirstOrDefaultAsync(t => t.Id == tableId);
            if (table == null) return false;

            return excludeOrderId.HasValue
                ? table.CurrentOrderId != null && table.CurrentOrderId != excludeOrderId
                : table.CurrentOrderId != null;
        }

        private async Task MarkTableNotificationsReadAsync(int tableId)
        {
            var unread = await _db.AdminNotifications
                .Where(n => n.TableId == tableId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread)
            {
                n.IsRead = true;
            }
        }

        public async Task TriggerNextSequenceAsync(int orderId, int? justServedItemId)
        {
            // 1. CHỐNG XUNG ĐỘT (Race Condition)
            var myLock = _orderLocks.GetOrAdd(orderId, _ => new SemaphoreSlim(1, 1));
            await myLock.WaitAsync();

            try
            {
                // SỬA LỖI: Load tường minh thực thể có kèm Navigation Properties để tính toán chính xác dữ liệu SortOrder & Priority
                var allItems = await _db.OrderDetails
                    .Include(od => od.Product)
                        .ThenInclude(p => p.Category)
                            .ThenInclude(c => c.Kitchen)
                    .Where(od => od.OrderId == orderId && od.Status != StatusProduct.Cancelled)
                    .ToListAsync();

                if (!allItems.Any()) return;

                // 2. TÌM NHÓM HIỆN TẠI (Nếu kích hoạt từ 1 món cụ thể)
                if (justServedItemId.HasValue)
                {
                    var currentServedItem = allItems.FirstOrDefault(x => x.Id == justServedItemId.Value);
                    if (currentServedItem != null)
                    {
                        int currentSortOrder = currentServedItem.Product?.Category?.Kitchen?.SortOrder ?? 0;
                        int currentPriority = currentServedItem.Product?.Category?.Priority ?? 0;

                        // Kiểm tra xem nhóm hiện tại đã lên HẾT ĐỦ các món chưa
                        bool isCurrentGroupFinished = !allItems.Any(od =>
                            (od.Product?.Category?.Kitchen?.SortOrder ?? 0) == currentSortOrder &&
                            (od.Product?.Category?.Priority ?? 0) == currentPriority &&
                            od.Status != StatusProduct.Served
                        );

                        // Nếu trong nhóm vẫn còn món chưa phục vụ xong -> Giữ phanh, chưa cho cuốn chiếu sang nhóm sau
                        if (!isCurrentGroupFinished) return;
                    }
                }

                // 3. ĐỊNH HÌNH TOÀN BỘ CÁC CẤP NHÓM TRONG ĐƠN HÀNG
                var groups = allItems
                    .GroupBy(od => new
                    {
                        SortOrder = od.Product?.Category?.Kitchen?.SortOrder ?? 0,
                        Priority = od.Product?.Category?.Priority ?? 0
                    })
                    .OrderBy(g => g.Key.SortOrder)
                    .ThenBy(g => g.Key.Priority)
                    .ToList();

                // 4. DUYỆT TÌM NHÓM TIẾP THEO HỢP LỆ ĐỂ ĐẨY XUỐNG BẾP
                foreach (var group in groups)
                {
                    var unfiredInGroup = group.Where(od => !od.IsFired).ToList();

                    if (unfiredInGroup.Any())
                    {
                        // Điều kiện: Toàn bộ các nhóm có thứ tự nhỏ hơn nhóm này bắt buộc phải ĐÃ PHỤC VỤ XONG hoàn toàn
                        bool allPreviousServed = groups
                            .Where(g => g.Key.SortOrder < group.Key.SortOrder ||
                                       (g.Key.SortOrder == group.Key.SortOrder && g.Key.Priority < group.Key.Priority))
                            .All(g => g.All(od => od.Status == StatusProduct.Served));

                        if (allPreviousServed)
                        {
                            await ExecuteFireGroupAsync(unfiredInGroup, "Cuốn chiếu tự động");
                            break; // Đạt mục đích bắn lệnh cho tầng kế tiếp -> Thoát luồng cuốn chiếu bước này
                        }
                    }
                }
            }
            finally
            {
                myLock.Release(); // Đảm bảo luôn giải phóng Lock tránh Deadlock
            }
        }

        // SỬA LỖI: Sử dụng strongly-typed List<OrderDetailsModel> thay thế hoàn toàn cho danh sách dynamic, triệt tiêu lỗi RuntimeBinderException
        private async Task ExecuteFireGroupAsync(List<OrderDetailsModel> itemsToFire, string fireType)
        {
            var now = DateTime.Now;
            bool isAnyFired = false;

            foreach (var item in itemsToFire)
            {
                item.IsFired = true;
                item.FiredAt = now;
                item.UpdatedAt = now;
                item.Status = StatusProduct.PreparingIngredient; // Đổi trạng thái sang bếp chuẩn bị nguyên liệu

                isAnyFired = true;
            }

            if (isAnyFired)
            {
                await _db.SaveChangesAsync();

                await BroadcastKitchenRefreshAsync();

                // Gửi thông báo Real-time cho từng món nhảy vào KDS của nhà bếp và thiết bị Admin
                var notifyTasks = itemsToFire.Select(item =>
                    _hub.Clients.Group(OrderHub.AdminGroup).SendAsync("DishFired", new
                    {
                        orderDetailId = item.Id,
                        productName = item.Product?.Name,
                        isRemake = (item.FireCount > 0),
                        status = (int)StatusProduct.PreparingIngredient,
                        message = $"[{fireType}]: Món {item.Product?.Name} đã được tự động chuyển xuống bếp!"
                    })
                );

                await Task.WhenAll(notifyTasks);
            }
        }
    }
}