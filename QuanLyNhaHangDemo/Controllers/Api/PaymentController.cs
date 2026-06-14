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

        public PaymentController(
            DataContext db,
            VNPayService vnPay,
            IHubContext<OrderHub> hub,
            ILogger<PaymentController> logger)
        {
            _db = db;
            _vnPay = vnPay;
            _hub = hub;
            _logger = logger;
        }

        // ──────────────────────────────────────────────────────────
        // GET /payment/vnpay-return  (VNPay redirect user về sau khi quét)
        // ──────────────────────────────────────────────────────────
        [HttpGet("vnpay-return")]
        public IActionResult VNPayReturn()
        {
            var vnp_ResponseCode = Request.Query["vnp_ResponseCode"].ToString();
            var vnp_TxnRef = Request.Query["vnp_TxnRef"].ToString();

            _logger.LogInformation(
                "[VNPay Return] ref={Ref} status={Status}",
                vnp_TxnRef, vnp_ResponseCode);

            bool checkSignature = _vnPay.ValidateSignature(Request.Query);

            if (checkSignature && vnp_ResponseCode == "00")
            {
                return Content("<html><body><h2>Thanh toán thành công! Vui lòng quay lại ứng dụng.</h2></body></html>", "text/html");
            }

            return Content("<html><body><h2>Thanh toán chưa hoàn tất hoặc lỗi chữ ký. Vui lòng thử lại.</h2></body></html>", "text/html");
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

            // 3. Tìm order theo VnPayReference
            var order = await _db.Orders
                .FirstOrDefaultAsync(o => o.VnPayReference == vnp_TxnRef);

            if (order == null)
            {
                _logger.LogWarning("[VNPay IPN] Không tìm thấy order với ref={Ref}", vnp_TxnRef);
                return Ok(new { RspCode = "01", Message = "Order not found" });
            }

            // 4. Tránh xử lý lặp
            if (order.PayStatus == PaymentStatus.Success)
            {
                _logger.LogInformation("[VNPay IPN] Order đã được thanh toán. ref={Ref}", vnp_TxnRef);
                return Ok(new { RspCode = "02", Message = "Order already confirmed" });
            }

            // 2. Chỉ xử lý nếu giao dịch thành công (status == "00")
            if (vnp_ResponseCode != "00")
            {
                _logger.LogInformation("[VNPay IPN] Giao dịch không thành công. status={St}", vnp_ResponseCode);
                return Ok(new { RspCode = "00", Message = "Confirm Success (but transaction failed)" });
            }

            // 2b. Kiểm tra số tiền giao dịch khớp với order để chống gian lận
            var vnp_Amount = Request.Query["vnp_Amount"].ToString();
            long expectedAmount = (long)Math.Round(order.GrandTotal) * 100;
            if (!string.IsNullOrEmpty(vnp_Amount) && long.TryParse(vnp_Amount, out long paidAmount))
            {
                if (paidAmount != expectedAmount)
                {
                    _logger.LogWarning("[VNPay IPN] Số tiền không khớp. Expected={Exp} Got={Got} ref={Ref}",
                        expectedAmount, paidAmount, vnp_TxnRef);
                    return Ok(new { RspCode = "04", Message = "Invalid amount" });
                }
            }

            // 5. Cập nhật trạng thái order
            order.PayStatus = PaymentStatus.Success;
            order.Status = OrderModel.OrderStatus.Paid;

            // 6. Giải phóng bàn
            var tables = await _db.Table
                .Where(t => t.CurrentOrderId == order.Id)
                .ToListAsync();

            int? tableId = null;
            foreach (var table in tables)
            {
                tableId = table.Id;
                table.Status = TableStatus.Empty;
                table.CurrentOrderId = null;
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "[VNPay IPN] Thanh toán thành công. OrderId={Id} TableId={TId}",
                order.Id, tableId);

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

                // Broadcast cập nhật sơ đồ bàn
                await _hub.Clients
                    .Group(OrderHub.FloorPlanGroup)
                    .SendAsync("FloorPlanRefresh", new { });
            }

            // 8. Trả "00" theo chuẩn VNPay
            return Ok(new { RspCode = "00", Message = "Confirm Success" });
        }
    }
}
