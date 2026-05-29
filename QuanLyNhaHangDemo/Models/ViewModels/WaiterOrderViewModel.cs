using System.Collections.Generic;

namespace QuanLyNhaHangDemo.Models.ViewModels
{
    public class WaiterOrderViewModel
    {
        public TableModel Table { get; set; }
        public OrderModel Order { get; set; }

        public List<OrderDetailsModel> OrderDetails { get; set; }
        public List<ProductModel> Products { get; set; }

        public int SelectedProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class WaiterAddItemModel
    {
        public string OrderCode { get; set; }
        public int TableId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
