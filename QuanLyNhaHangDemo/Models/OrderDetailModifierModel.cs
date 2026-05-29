namespace QuanLyNhaHangDemo.Models
{
    public class OrderDetailModifierModel
    {
        public int Id { get; set; }

        public int OrderDetailId { get; set; }
        public OrderDetailsModel OrderDetail { get; set; }

        public int ModifierId { get; set; }
        public ModifierModel Modifier { get; set; }

        public decimal ModifierPrice { get; set; }
    }
}
