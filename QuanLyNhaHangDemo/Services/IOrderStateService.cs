using QuanLyNhaHangDemo.Models;

namespace QuanLyNhaHangDemo.Services
{
    public interface IOrderStateService
    {
        Task SyncAfterOrderDetailStatusChangeAsync(int orderDetailId, StatusProduct oldStatus, StatusProduct newStatus);
        Task SyncAfterOrderStatusChangeAsync(int orderId, OrderModel.OrderStatus oldStatus, OrderModel.OrderStatus newStatus);
        Task<int> GetReadyItemCountAsync(int tableId);
        Task MarkTableItemsServedAsync(int tableId);
        Task<Dictionary<int, int>> GetAllTableReadyCountsAsync();
        Task<(bool Success, string Message)> FireOrderDetailAsync(int orderDetailId, bool isRemake);
        Task<(bool Success, string Message)> CheckoutTableAsync(int tableId);
        Task TriggerNextSequenceAsync(int orderId);
        Task NotifyNewOrderAsync(int orderId);
    }
}
