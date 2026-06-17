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

            // Thông báo khi đơn được thanh toán
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

        /// <summary>
        /// Thông báo khi có đơn hàng mới được tạo (Android đặt đơn)
        /// </summary>
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

        /// <summary>
        /// Thông báo khi đơn đã thanh toán
        /// </summary>
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

        /// <summary>
        /// Thông báo khi có món phải làm lại
        /// </summary>
        private async Task NotifyDishRemakeAsync(OrderDetailsModel detail)
        {
            var tables = await _db.Table
                .Include(t => t.Zone)
                .Where(t => t.CurrentOrderId == detail.OrderId)
                .ToListAsync();

            string tableInfo = tables.Any()
                ? string.Join(", ", tables.Select(t => $"Bàn {t.TableName}"))
                : "";

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
            detail.IsManuallyFired = true;

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

            if (isRemake)
            {
                await NotifyDishRemakeAsync(detail);
            }

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

        public async Task TriggerNextSequenceAsync(int orderId)
        {
            // 1. CHỐNG XUNG ĐỘT (Race Condition): Khóa độc lập theo từng đơn hàng
            var myLock = _orderLocks.GetOrAdd(orderId, _ => new System.Threading.SemaphoreSlim(1, 1));
            await myLock.WaitAsync();

            try
            {
                // 2. TỐI ƯU HIỆU NĂNG: Lấy dữ liệu ẩn danh, kết hợp Join sang Product -> Category -> Kitchen
                var allItems = await _db.OrderDetails
                    .Where(od => od.OrderId == orderId && od.Status != StatusProduct.Cancelled)
                    .Select(od => new
                    {
                        od.Id,
                        od.IsFired,
                        od.Status,
                        od.FireCount,
                        IsAutoFire = od.Product.Category.isAutoFire, // Hệ thống tự động kích hoạt
                        SortOrder = od.Product.Category.Kitchen.SortOrder,
                        Priority = od.Product.Category.Priority,
                        ProductName = od.Product.Name,
                        RawEntity = od // Giữ reference để cập nhật trực tiếp xuống DB
                    })
                    .ToListAsync();

                // 3. XỬ LÝ NHÓM ĐẶC QUYỀN (Món tự động AutoFire hoặc món được bấm nút Remake/Làm lại)
                // Món Làm Lại được nhận diện khi FireCount > 0 nhưng trạng thái bị reset về Pending/PreparingIngredient
                var privilegedItems = allItems.Where(od => !od.IsFired &&
                    (od.IsAutoFire || od.FireCount > 0)
                ).ToList();

                if (privilegedItems.Any())
                {
                    await ExecuteFireGroupAsync(privilegedItems, "Hệ thống Auto-Fire / Đồ làm lại");
                }

                // 4. XỬ LÝ CHUỖI CUỐN CHIẾU (Các món bình thường còn lại - IsAutoFire == false)
                var sequenceItems = allItems.Where(od => !od.IsAutoFire).ToList();
                var unfiredSequenceItems = sequenceItems.Where(od => !od.IsFired).ToList();

                if (!unfiredSequenceItems.Any())
                    return; // Toàn bộ món thường đã được kích hoạt xuống bếp

                // Xác định nhóm tiếp theo có độ ưu tiên thấp nhất đang chờ duyệt
                var nextGroup = unfiredSequenceItems
                    .OrderBy(od => od.SortOrder)
                    .ThenBy(od => od.Priority)
                    .GroupBy(od => new { od.SortOrder, od.Priority })
                    .FirstOrDefault();

                if (nextGroup == null)
                    return;

                // 5. THUẬT TOÁN QUÉT ĐIỀU KIỆN "MỞ XÍCH" XUỐNG BẾP
                // Tìm toàn bộ các món tuyến trước có SortOrder hoặc Priority cao hơn nhóm hiện tại
                var highPriorityItems = sequenceItems
                    .Where(od => od.SortOrder < nextGroup.Key.SortOrder ||
                                (od.SortOrder == nextGroup.Key.SortOrder && od.Priority < nextGroup.Key.Priority))
                    .ToList();

                bool isAllowedToFire = false;

                if (!highPriorityItems.Any())
                {
                    // Trường hợp A: Không có món nào đi trước -> Đây là nhóm đầu chuỗi (Khai vị lượt 1)
                    // Tự động xả xích cho phép xuống bếp luôn
                    isAllowedToFire = true;
                }
                else
                {
                    // Trường hợp B: Có nhóm đi trước. Điều kiện để nhóm sau được tự động xuống bếp:
                    // TẤT CẢ món tuyến trước đã được Fire VÀ có ÍT NHẤT 1 món tuyến trước đã dọn ra bàn (StatusProduct.Served)
                    bool allPrecedingAreFired = highPriorityItems.All(od => od.IsFired);
                    bool anyPrecedingIsServed = highPriorityItems.Any(od => od.Status == StatusProduct.Served);

                    if (allPrecedingAreFired && anyPrecedingIsServed)
                    {
                        isAllowedToFire = true;
                    }
                }

                // 6. KÍCH HOẠT XUỐNG BẾP
                if (isAllowedToFire)
                {
                    await ExecuteFireGroupAsync(nextGroup.ToList(), "Cuốn chiếu tự động");
                }
            }
            finally
            {
                myLock.Release(); // Giải phóng lock giải tỏa nghẽn mạch luồng dữ liệu
            }
        }

        // Hàm Helper thực thi cập nhật trạng thái và phát tín hiệu SignalR
        private async Task ExecuteFireGroupAsync(IEnumerable<dynamic> itemsToFire, string fireType)
        {
            var now = DateTime.Now; // Đồng bộ theo DateTime.Now giống như cấu hình khởi tạo CreateDate của bạn
            bool isAnyFired = false;

            foreach (var item in itemsToFire)
            {
                item.RawEntity.IsFired = true;
                item.RawEntity.FiredAt = now;
                item.RawEntity.UpdatedAt = now;

                // Chuyển trạng thái từ Chờ (Pending) sang Bếp chuẩn bị nguyên liệu (PreparingIngredient)
                item.RawEntity.Status = StatusProduct.PreparingIngredient;

                isAnyFired = true;
            }

            if (isAnyFired)
            {
                // Lưu xuống Database
                await _db.SaveChangesAsync();

                // Kích hoạt hàm làm mới màn hình KDS tổng của nhà bếp (bạn tự định nghĩa hàm này)
                await BroadcastKitchenRefreshAsync();

                // Gửi SignalR real-time thông báo cho từng món nhảy xuống các máy trạm (Tablet phục vụ / Web Admin)
                var notifyTasks = itemsToFire.Select(item =>
                    _hub.Clients.Group(OrderHub.AdminGroup).SendAsync("DishFired", new
                    {
                        orderDetailId = item.Id,
                        productName = item.ProductName,
                        isRemake = (item.FireCount > 0),
                        status = (int)StatusProduct.PreparingIngredient,
                        message = $"[{fireType}]: Món {item.ProductName} đã được chuyển xuống bếp!"
                    })
                );

                await Task.WhenAll(notifyTasks);
            }
        }
    }
}

