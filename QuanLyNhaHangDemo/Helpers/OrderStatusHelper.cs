using QuanLyNhaHangDemo.Models;

namespace QuanLyNhaHangDemo.Helpers
{
    public static class OrderStatusHelper
    {
        public static string GetItemStatusLabel(StatusProduct status) => status switch
        {
            StatusProduct.Pending => "Chờ bếp",
            StatusProduct.Cooking => "Đang chế biến",
            StatusProduct.Done => "Sẵn sàng phục vụ",
            StatusProduct.Served => "Đã phục vụ",
            StatusProduct.Cancelled => "Đã hủy",
            _ => "Không xác định"
        };

        public static string GetItemStatusBadgeClass(StatusProduct status) => status switch
        {
            StatusProduct.Pending => "bg-secondary",
            StatusProduct.Cooking => "bg-warning text-dark",
            StatusProduct.Done => "bg-success",
            StatusProduct.Served => "bg-info",
            StatusProduct.Cancelled => "bg-danger",
            _ => "bg-secondary"
        };

        public static string GetOrderStatusLabel(OrderModel.OrderStatus status) => status switch
        {
            OrderModel.OrderStatus.Pending => "Chờ bếp",
            OrderModel.OrderStatus.Serving => "Đang phục vụ",
            OrderModel.OrderStatus.Paid => "Đã thanh toán",
            OrderModel.OrderStatus.Completed => "Hoàn thành",
            OrderModel.OrderStatus.Cancelled => "Đã hủy",
            _ => "Không xác định"
        };

        public static string GetOrderStatusBadgeClass(OrderModel.OrderStatus status) => status switch
        {
            OrderModel.OrderStatus.Pending => "bg-secondary",
            OrderModel.OrderStatus.Serving => "bg-primary",
            OrderModel.OrderStatus.Paid => "bg-success",
            OrderModel.OrderStatus.Completed => "bg-info",
            OrderModel.OrderStatus.Cancelled => "bg-danger",
            _ => "bg-secondary"
        };

        public static string GetKitchenStatusLabel(StatusProduct status) => status switch
        {
            StatusProduct.Pending => "Đang chuẩn bị nguyên liệu",
            StatusProduct.Cooking => "Đang làm",
            StatusProduct.Done => "Hoàn thành",
            StatusProduct.Served => "Đã phục vụ",
            StatusProduct.Cancelled => "Đã hủy",
            _ => "Không xác định"
        };
    }
}
