using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyNhaHangDemo.Models
{
    public class OrderModel
    {
        public int Id { get; set; }
        public string OrderCode { get; set; }

        public decimal ShippingCost { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string UserName { get; set; }
        public int Status { get; set; }

        public decimal SubTotal { get; set; }
        public int? CouponId { get; set; }
        public CouponModel? Coupon { get; set; }
        public decimal DiscountAmount { get;set;  }
        public decimal VATRate { get; set; }
        public decimal VATAmount { get; set; }

        public decimal ServiceRate { get; set; }
        public decimal ServiceAmount { get; set; }

        public decimal GrandTotal {  get; set; }

        public int? TableId { get; set; }      
        public TableModel? Table { get; set; }

        
    }
}
