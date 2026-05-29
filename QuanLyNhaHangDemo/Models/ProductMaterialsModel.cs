namespace QuanLyNhaHangDemo.Models
{
    public class ProductMaterialsModel
    {
        public int ProductId { get; set; }
        public ProductModel Product { get; set; }
        public int MaterialId { get; set; }
        public MaterialModel Material { get; set; }
        public decimal QuantityRequired { get; set; }
    }
}
