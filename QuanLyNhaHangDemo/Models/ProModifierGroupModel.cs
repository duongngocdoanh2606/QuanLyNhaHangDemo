namespace QuanLyNhaHangDemo.Models
{
    public class ProModifierGroupModel
    {
        public int ProductId { get; set; }
        public ProductModel Product { get; set; }
        public int ModifierGroupId { get; set; }
        public ModifierGroupModel ModifierGroup { get; set; }

    }
}
