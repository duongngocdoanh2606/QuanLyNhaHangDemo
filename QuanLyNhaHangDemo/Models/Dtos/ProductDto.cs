namespace QuanLyNhaHangDemo.Models.Dtos
{
    public class ProductDto
    {
        public int Id { get; set; }
        public String Name { get; set; }
        public decimal Price { get; set; }
        public String Image { get; set; }
        public String Description { get; set; }
        public int CategoryId { get; set; }
    }
    public class ProductDetailDto
    {
        public int Id { get; set; }
        public String Name { get; set; }
        public String Description { get; set; }
        public decimal Price { get; set; }
        public String Image { get; set; }
        
    }
}
