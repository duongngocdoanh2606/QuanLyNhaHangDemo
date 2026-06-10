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

            if (oldStatus != StatusProduct.Served &&
                newStatus == StatusProduct.Served)
            {
                // Call trigger next sequence logic before saving the DB context.
                // Wait, it is better to save changes first, then trigger, or just trigger.
                // It's safe to trigger here since TriggerNextSequenceAsync has its own SaveChangesAsync.
                await TriggerNextSequenceAsync(order.Id);
            }

            await _db.SaveChangesAsync();

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

            if (newStatus == OrderModel.OrderStatus.Paid ||
                newStatus == OrderModel.OrderStatus.Cancelled)
            {
                var tables = await _db.Table
                    .Where(t => t.CurrentOrderId == order.Id)
                    .ToListAsync();

                foreach (var table in tables)
                {
                    table.Status = TableStatus.Empty;
                    table.CurrentOrderId = null;

                    await MarkTableNotificationsReadAsync(table.Id);
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

            foreach (var item in readyItems)
            {
                item.Status = StatusProduct.Served;
            }

            await MarkTableNotificationsReadAsync(tableId);

            await _db.SaveChangesAsync();

            await BroadcastFloorPlanUpdateAsync();

            await TriggerNextSequenceAsync(orderId.Value);
        }

        private async Task SyncOrderStatusFromDetailsAsync(
            OrderModel order)
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
                s == StatusProduct.Cooking);

            bool hasReady = details.Any(s =>
                s == StatusProduct.Done);

            bool allServedOrDone = details.All(s =>
                s == StatusProduct.Served ||
                s == StatusProduct.Done);

            if (hasActiveKitchen || hasReady)
            {
                if (order.Status == OrderModel.OrderStatus.Pending)
                {
                    order.Status = OrderModel.OrderStatus.Serving;
                }
            }
            else if (allServedOrDone &&
                     order.Status == OrderModel.OrderStatus.Serving)
            {
                // giữ Serving tới khi thanh toán
            }
        }

        private async Task SyncTableStatusAsync(int tableId)
        {
            var table = await _db.Table
                .FirstOrDefaultAsync(t => t.Id == tableId);

            if (table == null)
                return;

            if (table.CurrentOrderId != null)
            {
                table.Status = TableStatus.Serving;
            }
            else
            {
                table.Status = TableStatus.Empty;
            }
        }

        private async Task NotifyDishReadyAsync(
            OrderDetailsModel detail)
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

                string zoneName =
                    table.Zone?.ZoneName ?? "Khu vực chung";

                string productName =
                    detail.Product?.Name ?? "Món";

                string notifMessage =
                    $"{zoneName} - Bàn {tableName}: {productName} sẵn sàng phục vụ";

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
                    readyCount =
                        await GetReadyItemCountAsync(table.Id)
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

        private async Task BroadcastFloorPlanUpdateAsync()
        {
            var statuses =
                await GetAllTableReadyCountsAsync();

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

        public async Task<(bool Success, string Message)>
            FireOrderDetailAsync(
                int orderDetailId,
                bool isRemake)
        {
            var detail = await _db.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order)
                .FirstOrDefaultAsync(od => od.Id == orderDetailId);

            if (detail == null)
                return (false, "Không tìm thấy món.");

            if (detail.Status == StatusProduct.Cancelled)
                return (false, "Món đã hủy.");

            if (detail.Order.Status ==
                    OrderModel.OrderStatus.Paid ||
                detail.Order.Status ==
                    OrderModel.OrderStatus.Cancelled)
            {
                return (false, "Đơn đã kết thúc.");
            }

            StatusProduct oldStatus = detail.Status;

            if (isRemake)
            {
                if (detail.Status != StatusProduct.Done &&
                    detail.Status != StatusProduct.Served)
                {
                    return (false,
                        "Chỉ làm lại món đã xong.");
                }

                detail.FireCount++;

                detail.Status = StatusProduct.Pending;
            }
            else if (detail.Status == StatusProduct.Done ||
                     detail.Status == StatusProduct.Served)
            {
                return (false,
                    "Món đã xong.");
            }

            detail.IsFired = true;

            detail.FiredAt = DateTime.Now;

            if (!isRemake &&
                detail.Status == StatusProduct.Pending)
            {
                detail.Status = StatusProduct.Cooking;
            }

            StatusProduct newStatus = detail.Status;

            await _db.SaveChangesAsync();

            await SyncAfterOrderDetailStatusChangeAsync(
                orderDetailId,
                oldStatus,
                newStatus);

            await BroadcastKitchenRefreshAsync();

            await _hub.Clients
                .Group(OrderHub.AdminGroup)
                .SendAsync("DishFired",
                    new
                    {
                        orderDetailId = detail.Id,
                        productName = detail.Product?.Name,
                        isRemake,
                        message = isRemake
                            ? $"Làm lại: {detail.Product?.Name}"
                            : $"Fire ưu tiên: {detail.Product?.Name}"
                    });

            return (
                true,
                isRemake
                    ? "Đã gửi yêu cầu làm lại món."
                    : "Đã fire món.");
        }

        public async Task<(bool Success, string Message)>
            CheckoutTableAsync(int tableId)
        {
            var orderId = await GetActiveOrderIdAsync(tableId);

            if (!orderId.HasValue)
            {
                return (
                    false,
                    "Không có đơn đang phục vụ.");
            }

            var order = await _db.Orders
                .FirstOrDefaultAsync(o =>
                    o.Id == orderId.Value);

            if (order == null)
            {
                return (
                    false,
                    "Không tìm thấy đơn.");
            }

            var oldStatus = order.Status;

            order.Status = OrderModel.OrderStatus.Paid;

            await _db.SaveChangesAsync();

            await SyncAfterOrderStatusChangeAsync(
                order.Id,
                oldStatus,
                OrderModel.OrderStatus.Paid);

            return (
                true,
                "Đã thanh toán.");
        }

        public async Task<Dictionary<int, int>>
            GetAllTableReadyCountsAsync()
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

            var orderIds = tables
                .Select(t => t.OrderId)
                .Distinct()
                .ToList();

            var readyByOrder = await _db.OrderDetails
                .Where(od =>
                    orderIds.Contains(od.OrderId) &&
                    od.Status == StatusProduct.Done)
                .GroupBy(od => od.OrderId)
                .Select(g => new
                {
                    OrderId = g.Key,
                    Count = g.Count()
                })
                .ToDictionaryAsync(
                    x => x.OrderId,
                    x => x.Count);

            var result = new Dictionary<int, int>();

            foreach (var table in tables)
            {
                int count =
                    readyByOrder.GetValueOrDefault(
                        table.OrderId,
                        0);

                if (count > 0)
                {
                    result[table.TableId] = count;
                }
            }

            return result;
        }

        private async Task<int?> GetActiveOrderIdAsync(
            int tableId)
        {
            return await _db.Table
                .Where(t =>
                    t.Id == tableId &&
                    t.CurrentOrderId != null)
                .Select(t => t.CurrentOrderId)
                .FirstOrDefaultAsync();
        }

        private async Task<bool> HasActiveOrderOnTableAsync(
            int tableId,
            int? excludeOrderId)
        {
            var table = await _db.Table
                .FirstOrDefaultAsync(t => t.Id == tableId);

            if (table == null)
                return false;

            if (excludeOrderId.HasValue)
            {
                return table.CurrentOrderId != null &&
                       table.CurrentOrderId != excludeOrderId;
            }

            return table.CurrentOrderId != null;
        }

        private async Task MarkTableNotificationsReadAsync(
            int tableId)
        {
            var unread = await _db.AdminNotifications
                .Where(n =>
                    n.TableId == tableId &&
                    !n.IsRead)
                .ToListAsync();

            foreach (var n in unread)
            {
                n.IsRead = true;
            }
        }

        private async Task TriggerNextSequenceAsync(int orderId)
        {
            var unfiredItems = await _db.OrderDetails
                .Include(od => od.Product)
                    .ThenInclude(p => p.Category)
                        .ThenInclude(c => c.Kitchen)
                .Where(od => od.OrderId == orderId && 
                             !od.IsFired && 
                             od.Status != StatusProduct.Cancelled)
                .ToListAsync();

            if (!unfiredItems.Any())
                return;

            var nextGroup = unfiredItems
                .Where(od => od.Product?.Category?.Kitchen != null)
                .OrderBy(od => od.Product.Category.Kitchen.SortOrder)
                .ThenBy(od => od.Product.Category.Priority)
                .GroupBy(od => new 
                { 
                    SortOrder = od.Product.Category.Kitchen.SortOrder, 
                    Priority = od.Product.Category.Priority 
                })
                .FirstOrDefault();

            if (nextGroup == null)
                return;

            bool isAnyFired = false;

            foreach (var detail in nextGroup)
            {
                detail.IsFired = true;
                detail.FiredAt = DateTime.Now;
                detail.Status = StatusProduct.Pending;
                isAnyFired = true;
            }

            if (isAnyFired)
            {
                await _db.SaveChangesAsync();
                await BroadcastKitchenRefreshAsync();

                foreach (var detail in nextGroup)
                {
                    await _hub.Clients.Group(OrderHub.AdminGroup).SendAsync("DishFired", new
                    {
                        orderDetailId = detail.Id,
                        productName = detail.Product?.Name,
                        isRemake = false,
                        message = $"Fire tự động: {detail.Product?.Name}"
                    });
                }
            }
        }
    }
}

