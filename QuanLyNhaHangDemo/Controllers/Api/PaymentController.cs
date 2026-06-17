using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Hubs;
using QuanLyNhaHangDemo.Models;
using QuanLyNhaHangDemo.Repository;
using QuanLyNhaHangDemo.Services;

namespace QuanLyNhaHangDemo.Controllers.Api
{
    [ApiController]
    [Route("payment")]
    public class PaymentController : ControllerBase
    {
        private readonly DataContext _db;
        private readonly VNPayService _vnPay;
        private readonly IHubContext<OrderHub> _hub;
        private readonly ILogger<PaymentController> _logger;
        private readonly IOrderStateService _orderState;

        public PaymentController(
            DataContext db,
            VNPayService vnPay,
            IHubContext<OrderHub> hub,
            ILogger<PaymentController> logger,
            IOrderStateService orderState)
        {
            _db = db;
            _vnPay = vnPay;
            _hub = hub;
            _logger = logger;
            _orderState = orderState;
        }

        // ──────────────────────────────────────────────────────────
        // GET /payment/vnpay-return  (VNPay redirect user về sau khi quét)
        // ──────────────────────────────────────────────────────────
        [HttpGet("vnpay-return")]
        public async Task<IActionResult> VNPayReturn()
        {
            var vnp_ResponseCode = Request.Query["vnp_ResponseCode"].ToString();
            var vnp_TxnRef = Request.Query["vnp_TxnRef"].ToString();

            _logger.LogInformation(
                "[VNPay Return] ref={Ref} status={Status}",
                vnp_TxnRef, vnp_ResponseCode);

            bool checkSignature = _vnPay.ValidateSignature(Request.Query);

            if (checkSignature && vnp_ResponseCode == "00")
            {
                // Fallback: Xử lý thanh toán ngay trên ReturnUrl nếu IPN chưa kịp chạy (hoặc chưa được cấu hình)
                await ProcessPaymentSuccess(vnp_TxnRef, Request.Query["vnp_Amount"].ToString());

                return Content("<html><head><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"></head><body style=\"text-align:center; padding:20px; font-family:sans-serif;\"><h2 style=\"color:green;\">Thanh toán thành công!</h2><p>Giao dịch đã hoàn tất. Vui lòng quay lại ứng dụng.</p></body></html>", "text/html");
            }

            return Content("<html><head><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"></head><body style=\"text-align:center; padding:20px; font-family:sans-serif;\"><h2 style=\"color:red;\">Thanh toán thất bại!</h2><p>Giao dịch chưa hoàn tất hoặc lỗi chữ ký.</p></body></html>", "text/html");
        }

        // ──────────────────────────────────────────────────────────
        // GET /payment/vnpay-ipn  (VNPay server-to-server callback)
        // ──────────────────────────────────────────────────────────
        [HttpGet("vnpay-ipn")]
        public async Task<IActionResult> VNPayIpn()
        {
            var vnp_ResponseCode = Request.Query["vnp_ResponseCode"].ToString();
            var vnp_TxnRef = Request.Query["vnp_TxnRef"].ToString();

            _logger.LogInformation(
                "[VNPay IPN] ref={Ref} status={St}",
                vnp_TxnRef, vnp_ResponseCode);

            // 1. Xác thực chữ ký
            bool checkSignature = _vnPay.ValidateSignature(Request.Query);
            if (!checkSignature)
            {
                _logger.LogWarning("[VNPay IPN] Chữ ký không hợp lệ. ref={Ref}", vnp_TxnRef);
                return Ok(new { RspCode = "97", Message = "Invalid signature" });
            }

            // 2. Chỉ xử lý nếu giao dịch thành công
            if (vnp_ResponseCode != "00")
            {
                _logger.LogInformation("[VNPay IPN] Giao dịch không thành công. status={St}", vnp_ResponseCode);
                return Ok(new { RspCode = "00", Message = "Confirm Success (but transaction failed)" });
            }

            // 3. Gọi hàm xử lý chung
            var result = await ProcessPaymentSuccess(vnp_TxnRef, Request.Query["vnp_Amount"].ToString());

            if (result == "OrderNotFound") return Ok(new { RspCode = "01", Message = "Order not found" });
            if (result == "AlreadyConfirmed") return Ok(new { RspCode = "02", Message = "Order already confirmed" });
            if (result == "InvalidAmount") return Ok(new { RspCode = "04", Message = "Invalid amount" });

            // 8. Trả "00" theo chuẩn VNPay
            return Ok(new { RspCode = "00", Message = "Confirm Success" });
        }

        private async Task<string> ProcessPaymentSuccess(string vnp_TxnRef, string vnp_Amount)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.VnPayReference == vnp_TxnRef);
            if (order == null) return "OrderNotFound";

            if (order.PayStatus == PaymentStatus.Success) return "AlreadyConfirmed";

            long expectedAmount = (long)Math.Round(order.GrandTotal) * 100;
            if (!string.IsNullOrEmpty(vnp_Amount) && long.TryParse(vnp_Amount, out long paidAmount))
            {
                if (paidAmount != expectedAmount) return "InvalidAmount";
            }

            var oldStatus = order.Status;

            // 5. Cập nhật trạng thái order
            order.PayStatus = PaymentStatus.Success;
            order.Status = OrderModel.OrderStatus.Paid;

            // Get tableId for Android notification before saving
            var tables = await _db.Table.Where(t => t.CurrentOrderId == order.Id).ToListAsync();
            int? tableId = tables.FirstOrDefault()?.Id;

            await _db.SaveChangesAsync();

            // Gọi SyncAfterOrderStatusChangeAsync để dọn dẹp bàn, gửi SignalR OrderPaid cho Admin và cập nhật FloorPlan
            await _orderState.SyncAfterOrderStatusChangeAsync(order.Id, oldStatus, order.Status);

            _logger.LogInformation("[ProcessPaymentSuccess] Thanh toán thành công. OrderId={Id} TableId={TId}", order.Id, tableId);

            // 7. Gửi SignalR event đến Android đang chờ
            if (tableId.HasValue)
            {
                string group = $"payment-{tableId.Value}";
                await _hub.Clients.Group(group).SendAsync("PaymentSuccess", new
                {
                    tableId = tableId.Value,
                    orderId = order.Id,
                    orderCode = order.OrderCode,
                    amount = order.GrandTotal,
                    message = "Thanh toán VNPay thành công!"
                });
            }

            return "Success";
        }
    }
}
