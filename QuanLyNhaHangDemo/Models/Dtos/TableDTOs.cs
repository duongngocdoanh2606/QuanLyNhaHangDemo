using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLyNhaHangDemo.Models.Dtos
{
    public class TableMapDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
    }

    public class TableDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public OrderSummaryDto? ActiveOrder { get; set; }
    }

    public class OrderSummaryDto
    {
        public string OrderCode { get; set; } = string.Empty;
        public string? GuestName { get; set; }
        public string CheckInTime { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal VATAmount { get; set; }
        public decimal ServiceAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public int VariantId { get; set; } // Đại diện cho Id của Size được chọn
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    // --- ĐÃ ĐỒNG BỘ ĐỂ NHẬN ĐƠN HÀNG TOÀN DIỆN TỪ ANDROID ---
    public class CreateOrderRequest
    {
        public string GuestName { get; set; }
        public string CouponCode { get; set; }
        public int TableId { get; set; }
        public List<OrderItemRequest> Items { get; set; } = new List<OrderItemRequest>();
        public string Note { get; set; }
    }

    // --- ĐÃ ĐỒNG BỘ CHI TIẾT MÓN ĂN GỬI KÈM SIZE/TOPPING ---
    public class OrderItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }

        // Trùng khớp với List<Integer> modifierIds bên Android
        public List<int> ModifierIds { get; set; } = new List<int>();
        
        public string? Note { get; set; }
    }

    // --- ĐÃ ĐỒNG BỘ ĐỂ GỌI THÊM MÓN VÀO BÀN ĐANG ĐỨNG ---
    public class AddItemRequest
    {
        [Required]
        public int ProductId { get; set; }

        public int VariantId { get; set; } // Cấu hình Size cho món gọi thêm

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        // Cho phép gửi kèm Topping khi gọi thêm món vào hóa đơn cũ
        public List<int> ModifierIds { get; set; } = new();
        
        public string? Note { get; set; }
    }

    public class UpdateTableStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }

    public class CheckoutResultDto
    {
        public string OrderCode { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string CheckInTime { get; set; } = string.Empty;
        public string CheckOutTime { get; set; } = string.Empty;
        public string PaymentUrl { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;
    }

    public class OrderDetailMoveRequest
    {
        public int ProductId { get; set; }
        public int VariantId { get; set; }
        public int Quantity { get; set; }
    }
}