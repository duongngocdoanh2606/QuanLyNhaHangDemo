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
    }
}
