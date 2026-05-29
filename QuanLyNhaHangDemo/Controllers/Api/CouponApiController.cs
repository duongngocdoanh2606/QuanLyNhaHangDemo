using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Repository;
using System.Threading.Tasks;

namespace QuanLyNhaHangDemo.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class CouponApiController : ControllerBase
    {
        private readonly DataContext _context;

        public CouponApiController(DataContext context)
        {
            _context = context;
        }

        [HttpPost("validate")]
        public async Task<IActionResult> Validate([FromBody] CouponCheckRequest request)
        {
            // Tìm mã khớp trong Database công thức không phân biệt chữ hoa chữ thường
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code.ToUpper() == request.Code.ToUpper() && c.IsActive);

            if (coupon == null)
            {
                return NotFound(new { success = false, message = "Mã giảm giá không hợp lệ!" });
            }

            decimal finalDiscount = coupon.DiscountAmount;
            // Nếu tiền giảm lớn hơn cả tiền hóa đơn thì chỉ giảm tối đa bằng tiền hóa đơn
            if (finalDiscount > request.OrderTotal)
                finalDiscount = request.OrderTotal;

            return Ok(new
            {
                isValid = true, // Thêm trường này để khớp với code check của Android
                success = true,
                code = coupon.Code,
                discountAmount = finalDiscount
            });
        }
    }

    public class CouponCheckRequest
    {
        public string Code { get; set; }
        public decimal OrderTotal { get; set; }
    }
}