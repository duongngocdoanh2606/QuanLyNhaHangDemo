namespace QuanLyNhaHangDemo.Models.Dtos
{
    public class ProductDto
    {
        public int Id { get; set; }
        public String Name { get; set; }
        public decimal Price { get; set; }
        public int Sold { get; set; }
        public String Image { get; set; }
        public int CategoryId { get; set; }
        public bool IsAvailable { get; set; }
        public String ImageUrl { get; set; }
        public String Description { get; set; }
    }
    public class ProductDetailDto : ProductDto
    {
        public List<ModifierGroupDto> ModifierGroups { get; set; }
    }

}
