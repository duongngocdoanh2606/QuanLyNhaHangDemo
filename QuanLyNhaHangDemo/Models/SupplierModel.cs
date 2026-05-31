
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyNhaHangDemo.Models
{
    public class SupplierModel
    {
        [Key]
        public int SupplierId { get; set; }

        [Required]
        [MaxLength(100)]
        public string SupplierName { get; set; }

        [EmailAddress]
        public string? SupplierEmail { get; set; }

        [MaxLength(500)]
        public string? SupplierAddress { get; set; }

        public SupplierStatus Status { get; set; } = SupplierStatus.Active;

        public int SupplierCategoryId { get; set; }
        [ForeignKey("SupplierCategoryId")]
        public SupplierCategoryModel SupplierCategory { get; set; }

        public ICollection<MaterialModel> Materials { get; set; }
    }

    public enum SupplierStatus
    {
        Active = 1,
        inactive = 0
    }
}
