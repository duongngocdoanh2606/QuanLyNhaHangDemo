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
        private readonly VTCPayService _vtcPay;
        private readonly IHubContext<OrderHub> _hub;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            DataContext db,
            VTCPayService vtcPay,
            IHubContext<OrderHub> hub,
            ILogger<PaymentController> logger)
        {
            _db = db;
            _vtcPay = vtcPay;
            _hub = hub;
            _logger = logger;
        }

        // ──────────────────────────────────────────────────────────
        // GET /payment/vtcpay-return  (VTCPay redirect user về sau khi quét)
        // ──────────────────────────────────────────────────────────
        [HttpGet("vtcpay-return")]
        public IActionResult VTCPayReturn(
            [FromQuery] string? reference_number,
            [FromQuery] string? status,
            [FromQuery] string? amount,
            [FromQuery] string? website_id,
            [FromQuery] string? signature)
        {
            _logger.LogInformation(
                "[VTCPay Return] ref={Ref} status={Status}",
                reference_number, status);

            // Return URL chỉ để redirect về trang thông báo;
            // xác nhận thanh toán chính thức qua IPN
            if (status == "1")
                return Content("<html><body><h2>Thanh toán thành công! Vui lòng quay lại ứng dụng.</h2></body></html>", "text/html");

            return Content("<html><body><h2>Thanh toán chưa hoàn tất. Vui lòng thử lại.</h2></body></html>", "text/html");
        }

        // ──────────────────────────────────────────────────────────
        // POST /payment/vtcpay-ipn  (VTCPay server-to-server callback)
        // ──────────────────────────────────────────────────────────
        [HttpPost("vtcpay-ipn")]
        [HttpGet("vtcpay-ipn")]   // VTCPay một số phiên bản dùng GET
        public async Task<IActionResult> VTCPayIpn(
            [FromQuery] string? website_id,
            [FromQuery] string? reference_number,
            [FromQuery] string? amount,
            [FromQuery] string? status,
            [FromQuery] string? signature,
            [FromForm] string? website_id_form,
            [FromForm] string? reference_number_form,
            [FromForm] string? amount_form,
            [FromForm] string? status_form,
            [FromForm] string? signature_form)
        {
            // Ưu tiên form, fallback sang query
            string ws  = website_id_form  ?? website_id  ?? "";
            string refNum = reference_number_form ?? reference_number ?? "";
            string amt = amount_form ?? amount ?? "";
            string st  = status_form ?? status ?? "";
            string sig = signature_form  ?? signature  ?? "";

            _logger.LogInformation(
                "[VTCPay IPN] ws={Ws} ref={Ref} amount={Amt} status={St}",
                ws, refNum, amt, st);

            // 1. Xác thực chữ ký
            if (!_vtcPay.VerifyIpnSignature(ws, refNum, amt, st, sig))
            {
                _logger.LogWarning("[VTCPay IPN] Chữ ký không hợp lệ. ref={Ref}", refNum);
                return Content("0"); // VTCPay yêu cầu trả "0" khi lỗi
            }

            // 2. Chỉ xử lý nếu giao dịch thành công (status == "1")
            if (st != "1")
            {
                _logger.LogInformation("[VTCPay IPN] Giao dịch không thành công. status={St}", st);
                return Content("1"); // Trả "1" để VTCPay biết đã nhận
            }

            // 3. Tìm order theo VtcPayReference
            var order = await _db.Orders
                .FirstOrDefaultAsync(o => o.VtcPayReference == refNum);

            if (order == null)
            {
                _logger.LogWarning("[VTCPay IPN] Không tìm thấy order với ref={Ref}", refNum);
                return Content("1");
            }

            // 4. Tránh xử lý lặp
            if (order.PayStatus == PaymentStatus.Success)
            {
                _logger.LogInformation("[VTCPay IPN] Order đã được thanh toán. ref={Ref}", refNum);
                return Content("1");
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
                "[VTCPay IPN] Thanh toán thành công. OrderId={Id} TableId={TId}",
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
                    message = "Thanh toán VTCPay thành công!"
                });

                // Broadcast cập nhật sơ đồ bàn
                await _hub.Clients
                    .Group(OrderHub.FloorPlanGroup)
                    .SendAsync("FloorPlanRefresh", new { });
            }

            // 8. Trả "1" theo chuẩn VTCPay
            return Content("1");
        }
    }
}
