using QuanLyNhaHangDemo.Models;
using System.ComponentModel.DataAnnotations.Schema;

public class OrderModel
{
    public int Id { get; set; }

    public string OrderCode { get; set; }

    public string GuestName { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public OrderStatus Status { get; set; }

    public decimal SubTotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal VATRate { get; set; }

    public decimal ServiceRate { get; set; }

    public string? Note { get; set; }

    public int? CouponId { get; set; }

    public CouponModel? Coupon { get; set; }

    public virtual ICollection<OrderDetailsModel> OrderDetails { get; set; }
        = new List<OrderDetailsModel>();

    [NotMapped]
    public decimal VATAmount => SubTotal * VATRate;

    [NotMapped]
    public decimal ServiceAmount => SubTotal * ServiceRate;

    [NotMapped]
    public decimal GrandTotal =>
        SubTotal
        + VATAmount
        + ServiceAmount
        - DiscountAmount;

    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
    public PaymentStatus PayStatus { get; set; } = PaymentStatus.Unpaid;

    /// <summary>Reference number gửi lên VTCPay, dùng để đối chiếu IPN callback</summary>
    public string? VtcPayReference { get; set; }

    public enum OrderStatus
    {
        Pending,   // Mới tạo đơn, chờ nhà bếp xác nhận
        Serving,   // Đang phục vụ tại bàn
        Paid,      // Đã thanh toán xong
        Completed, // Đơn đã hoàn thành (đã thanh toán và khách đã rời đi)
        Cancelled  // Đã hủy đơn
    }
}

public enum PaymentMethod
{
    Cash = 1,
    VTCPay = 2
}

public enum PaymentStatus
{
    Unpaid = 0,
    Success = 1,
    Failed = 2
}