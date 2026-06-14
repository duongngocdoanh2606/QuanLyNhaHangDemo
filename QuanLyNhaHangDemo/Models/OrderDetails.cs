using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

namespace QuanLyNhaHangDemo.Models
{
    public class OrderDetailsModel
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.Now;

        // Mới: trạng thái chi tiết món theo luồng bếp mong muốn
        public StatusProduct Status { get; set; } = StatusProduct.Pending;

        /// <summary>Đã gửi xuống bếp (fire) — món hold chờ quản lý bấm Fire.</summary>
        public bool IsFired { get; set; }

        /// <summary>Cờ đánh dấu món này được Fire thủ công, tách biệt khỏi luồng trigger tự động.</summary>
        public bool IsManuallyFired { get; set; } = false;

        /// <summary>Số lần làm lại (remake).</summary>
        public int FireCount { get; set; }

        /// <summary>Thời điểm fire / làm lại gần nhất (ưu tiên trên KDS).</summary>
        public DateTime? FiredAt { get; set; }

        public decimal UnitPrice { get; set; }

        public string? Note { get; set; }

        [ForeignKey("ProductId")]
        public virtual ProductModel Product { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<OrderDetailModifierModel> OrderDetailModifiers { get; set; } = new List<OrderDetailModifierModel>();

        // --- CẬP NHẬT FOREIGN KEY CHO ORDER ---
        public int OrderId { get; set; }
        [ForeignKey("OrderId")]
        public virtual OrderModel Order { get; set; }
    }

    public enum StatusProduct
    {
        Pending = 0,               // Đang chờ (hold) — nhân viên/thu ngân có thể fire
        PreparingIngredient = 1,   // Nhân viên bếp chuẩn bị nguyên liệu
        Cooking = 2,               // Đang nấu
        Done = 3,                  // Đã nấu xong, chờ phục vụ bấm Served
        Served = 4,                // Đã dọn ra bàn (được bấm Served)
        Cancelled = 5              // Đã hủy
    }
}