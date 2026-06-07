using Microsoft.AspNetCore.SignalR;

namespace QuanLyNhaHangDemo.Hubs
{
    public class OrderHub : Hub
    {
        public const string AdminGroup = "admins";
        public const string FloorPlanGroup = "floor-plan";

        public async Task JoinAdmin()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, AdminGroup);
            await Groups.AddToGroupAsync(Context.ConnectionId, FloorPlanGroup);
        }

        /// <summary>
        /// Android client gọi để lắng nghe thanh toán của bàn cụ thể
        /// </summary>
        public async Task JoinPaymentGroup(int tableId)
        {
            string groupName = $"payment-{tableId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task LeavePaymentGroup(int tableId)
        {
            string groupName = $"payment-{tableId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }
    }
}
