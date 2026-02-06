using System.ComponentModel.DataAnnotations;

namespace QuanLyNhaHangDemo.Models
{
    public enum DiscountTypeEnum
    {
        Percentage = 1,
        FixedAmount = 2
    }

    public class CouponModel
    {
        public int Id { get; set; }

        [Required]
        public string Code { get; set; }

        public string Name { get; set; }

        public DiscountTypeEnum DiscountType { get; set; }

        public decimal DiscountValue { get; set; }

        public decimal? MinOrderAmount { get; set; }

        public decimal? MaxDiscountAmount { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int UsageLimit { get; set; }

        public int UsedCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreateAt { get; set; } = DateTime.Now;
    }
}
