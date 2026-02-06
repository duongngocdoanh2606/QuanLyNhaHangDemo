using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyNhaHangDemo.Models
{
    public class OrderDetails
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string OrderCode { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.Now;
        public StatusProduct Status { get; set; } = StatusProduct.Pending;
        public decimal Price { get; set; }
        [ForeignKey("ProductId")]
        public ProductModel Product { get; set; }

    }
    public enum StatusProduct 
    {
        Pending = 0,
        Cooking = 1,
        Done = 2,
        Cancelled = 3
    }

}
