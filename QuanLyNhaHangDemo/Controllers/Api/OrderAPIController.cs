using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Models;
using QuanLyNhaHangDemo.Models.Dtos;
using QuanLyNhaHangDemo.Repository;
using QuanLyNhaHangDemo.Services;

namespace QuanLyNhaHangDemo.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersApiController : ControllerBase
    {
        private readonly DataContext _db;
        private readonly IOrderStateService _orderState;

        public OrdersApiController(
            DataContext db,
            IOrderStateService orderState)
        {
            _db = db;
            _orderState = orderState;
        }

        [HttpPost("tables/{tableId:int}")]
        public async Task<ActionResult> CreateOrder(
            int tableId,
            [FromBody] CreateOrderRequest req)
        {
            var table = await _db.Table
                .FirstOrDefaultAsync(t => t.Id == tableId);

            if (table == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy bàn."
                });
            }

            if (table.CurrentOrderId != null)
            {
                return Conflict(new
                {
                    message = "Bàn đang có khách."
                });
            }

            var productIds = req.Items
                .Select(x => x.ProductId)
                .Distinct()
                .ToList();

            var products = await _db.Products
                .Include(x => x.Category)
                .Where(x => productIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            var modifierIds = req.Items
                .Where(x => x.ModifierIds != null)
                .SelectMany(x => x.ModifierIds)
                .Distinct()
                .ToList();

            var modifiers = await _db.Modifiers
                .Where(x => modifierIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            int? validCouponId = null;

            decimal couponDiscountConfig = 0;

            if (!string.IsNullOrEmpty(req.CouponCode))
            {
                var coupon = await _db.Coupons
                    .FirstOrDefaultAsync(c =>
                        c.Code.ToUpper() ==
                        req.CouponCode.ToUpper()
                        && c.IsActive);

                if (coupon != null)
                {
                    validCouponId = coupon.Id;

                    couponDiscountConfig =
                        coupon.DiscountAmount;
                }
            }

            var order = new OrderModel
            {
                OrderCode = Guid.NewGuid()
                    .ToString()
                    .Substring(0, 8)
                    .ToUpper(),

                GuestName = req.GuestName,

                Note = req.Note,

                Status = OrderModel.OrderStatus.Pending,

                CreatedDate = DateTime.Now,

                VATRate = 0.08m,

                ServiceRate = 0.05m,

                CouponId = validCouponId,

                DiscountAmount = 0
            };

            decimal subtotal = 0;

            foreach (var item in req.Items)
            {
                if (!products.TryGetValue(
                        item.ProductId,
                        out var product))
                {
                    continue;
                }

                decimal unitPrice = product.Price;

                bool autoFire =
                    product.Category?.isAutoFire ?? true;

                var orderDetail =
                    new OrderDetailsModel
                    {
                        Order = order,

                        ProductId = item.ProductId,

                        Quantity = item.Quantity,

                        CreateDate = DateTime.Now,

                        Status = StatusProduct.Pending,

                        IsFired = autoFire,

                        FiredAt = autoFire
                            ? DateTime.Now
                            : null,

                        Note = item.Note
                    };

                if (item.ModifierIds != null &&
                    item.ModifierIds.Any())
                {
                    foreach (var modId in item.ModifierIds)
                    {
                        if (!modifiers.TryGetValue(
                                modId,
                                out var mod))
                        {
                            continue;
                        }

                        unitPrice += mod.Price;

                        orderDetail
                            .OrderDetailModifiers
                            .Add(
                                new OrderDetailModifierModel
                                {
                                    ModifierId = mod.Id,
                                    ModifierPrice = mod.Price
                                });
                    }
                }

                orderDetail.UnitPrice = unitPrice;

                subtotal +=
                    unitPrice * item.Quantity;

                _db.OrderDetails.Add(orderDetail);
            }

            order.SubTotal = subtotal;

            if (order.CouponId.HasValue)
            {
                order.DiscountAmount =
                    couponDiscountConfig > subtotal
                    ? subtotal
                    : couponDiscountConfig;
            }

            _db.Orders.Add(order);

            await _db.SaveChangesAsync();

            // GÁN ORDER CHO BÀN
            table.CurrentOrderId = order.Id;

            table.Status = TableStatus.Serving;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                order.Id,
                order.OrderCode,
                order.SubTotal,
                order.DiscountAmount,
                order.GrandTotal
            });
        }

        [HttpPost("tables/{tableId:int}/orders/{orderCode}/items")]
        public async Task<ActionResult> AddItems(
            [FromRoute] int tableId,
            [FromRoute] string orderCode,
            [FromBody] List<AddItemRequest> items)
        {
            var table = await _db.Table
                .Include(t => t.CurrentOrder)
                .FirstOrDefaultAsync(t =>
                    t.Id == tableId);

            if (table == null ||
                table.CurrentOrder == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy bàn."
                });
            }

            var order = table.CurrentOrder;

            if (order.OrderCode != orderCode)
            {
                return NotFound(new
                {
                    message = "Sai mã order."
                });
            }

            if (order.Status !=
                    OrderModel.OrderStatus.Pending &&
                order.Status !=
                    OrderModel.OrderStatus.Serving)
            {
                return BadRequest(new
                {
                    message = "Order đã đóng."
                });
            }

            var productIds = items
                .Select(i => i.ProductId)
                .ToList();

            var products = await _db.Products
                .Include(p => p.Category)
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            var modifierIds = items
                .Where(x => x.ModifierIds != null)
                .SelectMany(x => x.ModifierIds)
                .Distinct()
                .ToList();

            var modifiers = await _db.Modifiers
                .Where(x => modifierIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            decimal totalAdded = 0;

            foreach (var item in items)
            {
                if (!products.TryGetValue(
                        item.ProductId,
                        out var prod))
                {
                    continue;
                }

                decimal unitPrice = prod.Price;

                bool autoFire =
                    prod.Category?.isAutoFire ?? true;

                var newOd =
                    new OrderDetailsModel
                    {
                        OrderId = order.Id,

                        ProductId = item.ProductId,

                        Quantity = item.Quantity,

                        Status = StatusProduct.Pending,

                        CreateDate = DateTime.Now,

                        IsFired = autoFire,

                        FiredAt = autoFire
                            ? DateTime.Now
                            : null,

                        Note = item.Note
                    };

                if (item.ModifierIds != null &&
                    item.ModifierIds.Any())
                {
                    foreach (var modId in item.ModifierIds)
                    {
                        if (!modifiers.TryGetValue(
                                modId,
                                out var mod))
                        {
                            continue;
                        }

                        unitPrice += mod.Price;

                        newOd.OrderDetailModifiers.Add(
                            new OrderDetailModifierModel
                            {
                                ModifierId = mod.Id,
                                ModifierPrice = mod.Price
                            });
                    }
                }

                newOd.UnitPrice = unitPrice;

                _db.OrderDetails.Add(newOd);

                totalAdded +=
                    item.Quantity * unitPrice;
            }

            order.SubTotal += totalAdded;

            if (order.CouponId.HasValue)
            {
                var coupon = await _db.Coupons
                    .FindAsync(order.CouponId.Value);

                if (coupon != null)
                {
                    order.DiscountAmount =
                        coupon.DiscountAmount >
                        order.SubTotal
                        ? order.SubTotal
                        : coupon.DiscountAmount;
                }
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Đã thêm món thành công."
            });
        }

        [HttpPost("tables/{tableId:int}/checkout")]
        public async Task<ActionResult<CheckoutResultDto>>
            Checkout(
                int tableId,
                [FromQuery] int paymentMethod = 1,
                [FromQuery] string? couponCode = null)
        {
            var table = await _db.Table
                .Include(t => t.CurrentOrder)
                .FirstOrDefaultAsync(t =>
                    t.Id == tableId);

            if (table == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy bàn."
                });
            }

            if (table.CurrentOrder == null)
            {
                return NotFound(new
                {
                    message = "Không có order."
                });
            }

            var order = table.CurrentOrder;

            if (order.Status !=
                    OrderModel.OrderStatus.Pending &&
                order.Status !=
                    OrderModel.OrderStatus.Serving)
            {
                return BadRequest(new
                {
                    message = "Order đã đóng."
                });
            }

            if (!string.IsNullOrEmpty(couponCode))
            {
                order.CouponId = null;

                order.DiscountAmount = 0;

                var coupon = await _db.Coupons
                    .FirstOrDefaultAsync(c =>
                        c.Code.ToUpper() ==
                        couponCode.ToUpper()
                        && c.IsActive);

                if (coupon != null)
                {
                    order.CouponId = coupon.Id;

                    order.DiscountAmount =
                        coupon.DiscountAmount >
                        order.SubTotal
                        ? order.SubTotal
                        : coupon.DiscountAmount;
                }
            }
            else if (order.CouponId.HasValue)
            {
                var coupon = await _db.Coupons
                    .FindAsync(order.CouponId.Value);

                if (coupon != null)
                {
                    order.DiscountAmount =
                        coupon.DiscountAmount >
                        order.SubTotal
                        ? order.SubTotal
                        : coupon.DiscountAmount;
                }
            }

            await _db.SaveChangesAsync();

            var (success, message) =
                await _orderState
                    .CheckoutTableAsync(tableId);

            if (!success)
            {
                return BadRequest(new
                {
                    message
                });
            }

            var updatedOrder = await _db.Orders
                .FindAsync(order.Id);

            updatedOrder.Method =
                paymentMethod == 2
                ? PaymentMethod.VTCPay
                : PaymentMethod.Cash;

            await _db.SaveChangesAsync();

            string paymentUrl = "";

            if (updatedOrder.Method ==
                PaymentMethod.VTCPay)
            {
                string websiteId = "55930";

                string amount =
                    ((int)updatedOrder.GrandTotal)
                    .ToString();

                string referenceNumber =
                    updatedOrder.OrderCode +
                    DateTime.Now.Ticks;

                paymentUrl =
                    $"https://sandbox.vtcpay.vn/portalgateway/checkout.html?amount={amount}&currency=VND&receiverAccount=0987654321&reference_number={referenceNumber}&website_id={websiteId}";
            }

            return Ok(new CheckoutResultDto
            {
                OrderCode = updatedOrder.OrderCode,

                TableName = table.TableName,

                TotalAmount =
                    updatedOrder.GrandTotal,

                CheckInTime =
                    updatedOrder.CreatedDate
                    .ToString("HH:mm dd/MM/yyyy"),

                CheckOutTime =
                    DateTime.Now
                    .ToString("HH:mm dd/MM/yyyy"),

                PaymentUrl = paymentUrl
            });
        }
    }
}

