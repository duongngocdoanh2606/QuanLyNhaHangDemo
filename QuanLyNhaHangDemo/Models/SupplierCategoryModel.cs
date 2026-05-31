using System.ComponentModel.DataAnnotations;

namespace QuanLyNhaHangDemo.Models
{
    public class SupplierCategoryModel
    {
        [Key]
        public int SupplierCategoryId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên danh mục nhà cung cấp")]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<SupplierModel> Suppliers { get; set; }
    }
}
