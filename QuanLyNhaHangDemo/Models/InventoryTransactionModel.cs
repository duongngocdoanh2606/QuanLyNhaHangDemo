using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
//Lịch sử nhập xuất kho
namespace QuanLyNhaHangDemo.Models
{
    public class InventoryTransactionModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int MaterialId { get; set; }
        public MaterialModel Material { get; set; }

        public DateTime DateCreated { get; set; }

        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }

        /// <summary>IN = Nhập kho | OUT = Xuất kho</summary>
        public string Type { get; set; }

        /// <summary>
        /// Lý do xuất kho:
        /// OUT_SALE   — Xuất do bán hàng (tự động khi đơn → Paid)
        /// OUT_RETURN — Xuất trả hàng lỗi về nhà sản xuất (thủ công)
        /// OUT_MANUAL — Xuất thủ công khác
        /// </summary>
        public string? Reason { get; set; }

        public string? Note { get; set; }

        public int? OrderId { get; set; }
        public OrderModel? Order { get; set; }

        /// <summary>NCC nhận hàng trả (chỉ dùng khi Reason = OUT_RETURN)</summary>
        public int? SupplierId { get; set; }
        public SupplierModel? Supplier { get; set; }
    }
}
