using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyNhaHangDemo.Models
{
    public class MaterialModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Tên nguyên liệu")]
        public string Name { get; set; }

        [Display(Name = "Đơn vị tính")]
        public string Unit { get; set; }

        [Display(Name = "Số lượng tồn")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentQuantity { get; set; }

        [Display(Name = "Ngưỡng cảnh báo")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ReorderLevel { get; set; }

        [Display(Name = "Trạng thái")]
        public int Status { get; set; } = 1;

        [DataType(DataType.Date)] 
        public DateTime? ExpiryDate { get; set; }
        
        [Required]
        [Display(Name = "Nhà cung cấp")]
        public int SupplierId { get; set; }

        public SupplierModel Supplier { get; set; }

        public ICollection<InventoryTransactionModel> InventoryTransactions { get; set; }
    }

    
}
