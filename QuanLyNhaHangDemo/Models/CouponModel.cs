using System.ComponentModel.DataAnnotations;

namespace QuanLyNhaHangDemo.Models
{
    public class CouponModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Code { get; set; } // Mã bạn tự gõ (Ví dụ: KHUYENMAI10, GIAM20K)

        [Required]
        public decimal DiscountAmount { get; set; } // Số tiền giảm (Ví dụ: 20000)

        public bool IsActive { get; set; } = true; // Trạng thái kích hoạt
    }
}