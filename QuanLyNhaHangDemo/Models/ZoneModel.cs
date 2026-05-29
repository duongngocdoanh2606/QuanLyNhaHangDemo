using System.ComponentModel.DataAnnotations;

namespace QuanLyNhaHangDemo.Models
{
    public class ZoneModel
    {
        [Key]
        public int ZoneId { get; set; }
        [Required(ErrorMessage ="Tên khu vực không được để trống")]
        public string ZoneName { get; set; }
        public string ZoneDescription { get; set; }
        public ZoneStatus ZoneStatus { get; set; }
            = ZoneStatus.Active;

        [Required(ErrorMessage = "Chiều rộng không được để trống")]
        [Range(100, 5000, ErrorMessage = "Chiều rộng phải từ 100px đến 5000px")]
        public int Width { get; set; } = 1200; // Giá trị mặc định gợi ý

        [Required(ErrorMessage = "Chiều cao không được để trống")]
        [Range(100, 5000, ErrorMessage = "Chiều cao phải từ 100px đến 5000px")]
        public int Height { get; set; } = 800; // Giá trị mặc định gợi ý

        public ICollection<TableModel> Tables { get; set; }
    }
    public enum ZoneStatus
    {
        Active = 0,

        Maintenance = 1,

        Closed = 2
    }
}
