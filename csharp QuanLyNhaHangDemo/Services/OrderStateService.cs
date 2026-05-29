using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Hubs;
using QuanLyNhaHangDemo.Models;
using QuanLyNhaHangDemo.Repository;

namespace QuanLyNhaHangDemo.Services
{
    public class OrderStateService : IOrderStateService
    {
        private readonly DataContext _db;
        private readonly IHubContext<OrderHub> _hub;

        public OrderStateService(DataContext db, IHubContext<OrderHub> hub)
        {
            _db = db;
            _hub = hub;
        }

        public async Task SyncAfterOrderDetailStatusChangeAsync(
            int orderDetailId, StatusProduct oldStatus, StatusProduct newStatus)
        {
            var detail = await _db.OrderDetails
                .Include(od => od.Product)
                    .ThenInclude(p => p.Category)
                .Include(od => od.Order)
                    .ThenInclude(o => o.Table)
                .FirstOrDefaultAsync(od => od.Id == orderDetailId);

            if (detail?.Order == null)
                return;

            var order = detail.Order;
            await SyncOrderStatusFromDetailsAsync(order);

            if (order.TableId.HasValue)
                await SyncTableStatusAsync(order.TableId.Value);

            if (oldStatus != StatusProduct.Done && newStatus == StatusProduct.Done)
                await NotifyDishReadyAsync(detail);

            // Nếu món được phục vụ (server trigger Served) hoặc hoàn thành => có thể auto-fire nhóm tiếp theo
            if (newStatus == StatusProduct.Served || newStatus == StatusProduct.Done)
            {
                var categoryPriority = detail.Product?.Category?.Priority;
                if (categoryPriority.HasValue)
                    await AutoFireNextPriorityIfNeeded(order, categoryPriority.Value);
            }

            await _db.SaveChangesAsync();
            await BroadcastFloorPlanUpdateAsync();
        }

        public async Task SyncAfterOrderStatusChangeAsync(
            int orderId, OrderModel.OrderStatus oldStatus, OrderModel.OrderStatus newStatus)
        {
            var order = await _db.Orders.FindAsync(orderId);
            if (order == null)
                return;

            if (newStatus == OrderModel.OrderStatus.Paid || newStatus == OrderModel.OrderStatus.Cancelled)
            {
                if (order.TableId.HasValue)
                {
                    var table = await _db.Table.FindAsync(order.TableId.Value);
                    if (table != null)
                    {
                        var hasOtherActive = await HasActiveOrderOnTableAsync(order.TableId.Value, orderId);
                        if (!hasOtherActive)
                            table.Status = TableStatus.Empty;
                    }

                    await MarkTableNotificationsReadAsync(order.TableId.Value);
                }
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
                .CountAsync(od => od.OrderId == orderId.Value
                    && od.Status == StatusProduct.Done);
        }

        public async Task MarkTableItemsServedAsync(int tableId)
        {
            var orderId = await GetActiveOrderIdAsync(tableId);
            if (!orderId.HasValue)
                return;

            var readyItems = await _db.OrderDetails
                .Where(od => od.OrderId == orderId.Value && od.Status == StatusProduct.Done)
                .ToListAsync();

            foreach (var item in readyItems)
                item.Status = StatusProduct.Served;

            await MarkTableNotificationsReadAsync(tableId);
            await _db.SaveChangesAsync();
            await BroadcastFloorPlanUpdateAsync();
        }

        private async Task SyncOrderStatusFromDetailsAsync(OrderModel order)
        {
            if (order.Status == OrderModel.OrderStatus.Paid
                || order.Status == OrderModel.OrderStatus.Cancelled
                || order.Status == OrderModel.OrderStatus.Completed)
                return;

            var details = await _db.OrderDetails
                .Where(od => od.OrderId == order.Id && od.Status != StatusProduct.Cancelled)
                .Select(od => od.Status)
                .ToListAsync();

            if (!details.Any())
                return;

            bool hasActiveKitchen = details.Any(s =>
                s == StatusProduct.Pending || s == StatusProduct.Cooking);
            bool hasReady = details.Any(s => s == StatusProduct.Done);
            bool allServedOrDone = details.All(s =>
                s == StatusProduct.Served || s == StatusProduct.Done);

            if (hasActiveKitchen || hasReady)
            {
                if (order.Status == OrderModel.OrderStatus.Pending)
                    order.Status = OrderModel.OrderStatus.Serving;
            }
            else if (allServedOrDone && order.Status == OrderModel.OrderStatus.Serving)
            {
                // Tất cả món đã xong hoặc đã phục vụ — giữ Serving cho đến khi thanh toán
            }
        }

        private async Task SyncTableStatusAsync(int tableId)
        {
            var table = await _db.Table.FindAsync(tableId);
            if (table == null)
                return;

            var hasActive = await HasActiveOrderOnTableAsync(tableId, null);
            if (hasActive)
                table.Status = TableStatus.Serving;
            else if (table.Status == TableStatus.Serving)
                table.Status = TableStatus.Empty;
        }

        private async Task NotifyDishReadyAsync(OrderDetailsModel detail)
        {
            if (!detail.Order.TableId.HasValue)
                return;

            int tableId = detail.Order.TableId.Value;
            string tableName = detail.Order.Table?.TableName ?? $"Bàn {tableId}";
            string productName = detail.Product?.Name ?? "Món";

            _db.AdminNotifications.Add(new AdminNotificationModel
            {
                TableId = tableId,
                OrderId = detail.OrderId,
                OrderDetailId = detail.Id,
                ProductName = productName,
                Message = $"Bàn {tableName}: {productName} sẵn sàng phục vụ",
                IsRead = false,
                CreatedAt = DateTime.Now
            });

            var payload = new
            {
                tableId,
                tableName,
                orderDetailId = detail.Id,
                productName,
                message = $"Bàn {tableName}: {productName} sẵn sàng phục vụ",
                readyCount = await GetReadyItemCountAsync(tableId)
            };

            await _hub.Clients.Group(OrderHub.AdminGroup)
                .SendAsync("DishReady", payload);

            await _hub.Clients.Group(OrderHub.FloorPlanGroup)
                .SendAsync("TableReadyUpdate", new { tableId, readyCount = payload.readyCount });
        }

        private async Task BroadcastFloorPlanUpdateAsync()
        {
            var statuses = await GetAllTableReadyCountsAsync();
            await _hub.Clients.Group(OrderHub.FloorPlanGroup)
                .SendAsync("FloorPlanRefresh", statuses);
        }

        private async Task BroadcastKitchenRefreshAsync()
        {
            await _hub.Clients.Group(OrderHub.AdminGroup)
                .SendAsync("KitchenRefresh");
        }

        public async Task<(bool Success, string Message)> FireOrderDetailAsync(int orderDetailId, bool isRemake)
        {
            var detail = await _db.OrderDetails
                .Include(od => od.Product)
                    .ThenInclude(p => p.Category)
                .Include(od => od.Order)
                    .ThenInclude(o => o.Table)
                .FirstOrDefaultAsync(od => od.Id == orderDetailId);

            if (detail == null)
                return (false, "Không tìm thấy món trong đơn.");

            if (detail.Status == StatusProduct.Cancelled)
                return (false, "Món đã hủy, không thể fire.");

            if (detail.Order.Status == OrderModel.OrderStatus.Paid
                || detail.Order.Status == OrderModel.OrderStatus.Cancelled)
                return (false, "Đơn đã kết thúc, không thể thay đổi.");

            StatusProduct oldStatus = detail.Status;

            if (isRemake)
            {
                if (detail.Status != StatusProduct.Done && detail.Status != StatusProduct.Served)
                    return (false, "Chỉ làm lại món đã hoàn thành hoặc đã phục vụ.");

                detail.FireCount++;
                detail.Status = StatusProduct.Pending;
            }
            else if (detail.Status == StatusProduct.Done || detail.Status == StatusProduct.Served)
            {
                return (false, "Món đã xong — dùng nút Làm lại nếu cần nấu lại.");
            }

            detail.IsFired = true;
            detail.FiredAt = DateTime.Now;

            if (!isRemake && detail.Status == StatusProduct.Pending)
                detail.Status = StatusProduct.Cooking;

            StatusProduct newStatus = detail.Status;
            await _db.SaveChangesAsync();
            await SyncAfterOrderDetailStatusChangeAsync(orderDetailId, oldStatus, newStatus);
            await BroadcastKitchenRefreshAsync();

            await _hub.Clients.Group(OrderHub.AdminGroup)
                .SendAsync("DishFired", new
                {
                    orderDetailId = detail.Id,
                    productName = detail.Product?.Name,
                    isRemake,
                    message = isRemake
                        ? $"Làm lại: {detail.Product?.Name}"
                        : $"Fire ưu tiên: {detail.Product?.Name}"
                });

            return (true, isRemake ? "Đã gửi yêu cầu làm lại món xuống bếp." : "Đã ưu tiên nấu món (Fire).");
        }

        public async Task<(bool Success, string Message)> CheckoutTableAsync(int tableId)
        {
            var order = await _db.Orders
                .Where(o => o.TableId == tableId
                    && (o.Status == OrderModel.OrderStatus.Pending
                        || o.Status == OrderModel.OrderStatus.Serving))
                .OrderByDescending(o => o.CreatedDate)
                .FirstOrDefaultAsync();

            if (order == null)
                return (false, "Không có đơn đang phục vụ tại bàn này.");

            var oldStatus = order.Status;
            order.Status = OrderModel.OrderStatus.Paid;

            await _db.SaveChangesAsync();
            await SyncAfterOrderStatusChangeAsync(order.Id, oldStatus, OrderModel.OrderStatus.Paid);

            return (true, "Đã thanh toán. Bàn đã chuyển sang trống.");
        }

        public async Task<Dictionary<int, int>> GetAllTableReadyCountsAsync()
        {
            var activeOrders = await _db.Orders
                .Where(o => o.TableId != null
                    && (o.Status == OrderModel.OrderStatus.Pending
                        || o.Status == OrderModel.OrderStatus.Serving))
                .Select(o => new { o.Id, TableId = o.TableId.Value })
                .ToListAsync();

            if (!activeOrders.Any())
                return new Dictionary<int, int>();

            var orderIds = activeOrders.Select(o => o.Id).ToList();
            var readyByOrder = await _db.OrderDetails
                .Where(od => orderIds.Contains(od.OrderId) && od.Status == StatusProduct.Done)
                .GroupBy(od => od.OrderId)
                .Select(g => new { OrderId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.OrderId, x => x.Count);

            var result = new Dictionary<int, int>();
            foreach (var ao in activeOrders)
            {
                int count = readyByOrder.GetValueOrDefault(ao.Id, 0);
                if (count > 0)
                    result[ao.TableId] = count;
            }
            return result;
        }

        private async Task<int?> GetActiveOrderIdAsync(int tableId)
        {
            return await _db.Orders
                .Where(o => o.TableId == tableId
                    && (o.Status == OrderModel.OrderStatus.Pending
                        || o.Status == OrderModel.OrderStatus.Serving))
                .OrderByDescending(o => o.CreatedDate)
                .Select(o => (int?)o.Id)
                .FirstOrDefaultAsync();
        }

        private async Task<bool> HasActiveOrderOnTableAsync(int tableId, int? excludeOrderId)
        {
            var query = _db.Orders.Where(o => o.TableId == tableId
                && (o.Status == OrderModel.OrderStatus.Pending
                    || o.Status == OrderModel.OrderStatus.Serving));

            if (excludeOrderId.HasValue)
                query = query.Where(o => o.Id != excludeOrderId.Value);

            return await query.AnyAsync();
        }

        private async Task MarkTableNotificationsReadAsync(int tableId)
        {
            var unread = await _db.AdminNotifications
                .Where(n => n.TableId == tableId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread)
                n.IsRead = true;
        }

        /// <summary>
        /// Khi tất cả món của một priority đã Done/Served, tự động fire nhóm priority tiếp theo (nếu category.isAutoFire == true).
        /// Fire theo nhóm: chỉ fire các món có cùng priority nhỏ nhất chưa được fire và có isAutoFire = true.
        /// </summary>
        private async Task AutoFireNextPriorityIfNeeded(OrderModel order, int completedPriority)
        {
            // Nếu vẫn còn món trong cùng priority chưa Done/Served --> dừng
            var hasRemainingInSame = await _db.OrderDetails
                .Include(od => od.Product)
                    .ThenInclude(p => p.Category)
                .Where(od => od.OrderId == order.Id
                    && od.Product.Category.Priority == completedPriority
                    && od.Status != StatusProduct.Done
                    && od.Status != StatusProduct.Served
                    && od.Status != StatusProduct.Cancelled)
                .AnyAsync();

            if (hasRemainingInSame)
                return;

            // Tìm các món chưa fire, chưa huỷ, có isAutoFire = true và priority > completedPriority
            var candidates = await _db.OrderDetails
                .Include(od => od.Product)
                    .ThenInclude(p => p.Category)
                .Where(od => od.OrderId == order.Id
                    && !od.IsFired
                    && od.Status != StatusProduct.Cancelled
                    && od.Product != null
                    && od.Product.Category != null
                    && od.Product.Category.isAutoFire == true
                    && od.Product.Category.Priority > completedPriority)
                .ToListAsync();

            if (!candidates.Any())
                return;

            // Fire nhóm có priority nhỏ nhất trong các ứng viên
            var nextPriority = candidates.Min(c => c.Product.Category.Priority);
            var groupToFire = candidates
                .Where(c => c.Product.Category.Priority == nextPriority)
                .ToList();

            foreach (var od in groupToFire)
            {
                od.IsFired = true;
                od.FiredAt = DateTime.Now;
                if (od.Status == StatusProduct.Pending)
                    od.Status = StatusProduct.Cooking;
            }

            await _db.SaveChangesAsync();
            await BroadcastKitchenRefreshAsync();
        }
    }
}