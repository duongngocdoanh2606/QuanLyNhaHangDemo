namespace QuanLyNhaHangDemo.Models.Dtos
{
    public class ModifierGroupDto
    {
        public int Id { get; set; }
        public string Name { get; set; } // Ví dụ: "Chọn Topping", "Mức đường"
        public string Type { get; set; } // Ví dụ: "Topping", "SugarLevel"
        public List<ModifierDto> Options { get; set; } = new List<ModifierDto>();
    }
    public class ModifierDto
    {
        public int Id { get; set; }
        public string Name { get; set; } // Ví dụ: "Trân châu đen"
        public decimal ExtraPrice { get; set; } // Giá cộng thêm (ví dụ: 5000)
        public bool IsAvailable { get; set; } = true;
    }
}
