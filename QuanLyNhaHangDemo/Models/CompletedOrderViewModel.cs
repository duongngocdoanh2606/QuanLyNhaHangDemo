using System;

namespace QuanLyNhaHangDemo.Models
{
    public class CompletedOrderViewModel
    {
        public string OrderCode { get; set; }
        public string UserName { get; set; }
        public DateTime CreatedDate { get; set; }

        // Mã chữ của Coupon (Ví dụ: GIAM20) - Lấy từ o.Coupon.Code
        public string CouponCode { get; set; }

        // Số tiền giảm giá - Ánh xạ từ o.DiscountAmount
        public decimal CouponDiscount { get; set; }

        // Tiền thuế VAT
        public decimal VATAmount { get; set; }

        // Phí dịch vụ
        public decimal ServiceAmount { get; set; }

        // Doanh thu từ tiền món gốc (SubTotal chưa thuế/giảm giá) - Ánh xạ từ o.SubTotal
        public decimal OrderRevenue { get; set; }

        // Tổng tiền thực tế thu từ khách sau khi tính toán - Ánh xạ từ o.GrandTotal
        public decimal TotalWithCoupon { get; set; }

        // Phương thức thanh toán
        public PaymentMethod Method { get; set; }

        public string PaymentMethodLabel => Method switch
        {
            PaymentMethod.VNPay => "VNPay",
            _ => "Tiền mặt"
        };

        public string PaymentMethodIcon => Method switch
        {
            PaymentMethod.VNPay => "fas fa-qrcode text-primary",
            _ => "fas fa-money-bill-wave text-success"
        };
    }
}