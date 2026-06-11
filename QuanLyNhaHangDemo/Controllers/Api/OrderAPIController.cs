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
        private readonly VNPayService _vnpPay;

        public OrdersApiController(
            DataContext db,
            IOrderStateService orderState,
            VNPayService vnpPay)
        {
            _db = db;
            _orderState = orderState;
            _vnpPay = vnpPay;
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

                VATRate = 0.10m,

                ServiceRate = 0.05m,

                CouponId = validCouponId,

                DiscountAmount = 0
            };

            decimal subtotal = 0;
            bool hasAutoFireItem = false;

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

                if (autoFire) hasAutoFireItem = true;

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

            if (!hasAutoFireItem)
            {
                var nuocLocProduct = await _db.Products.FirstOrDefaultAsync(p => p.Name.ToLower() == "nước lọc");
                if (nuocLocProduct == null)
                {
                    var autoCat = await _db.Categories.FirstOrDefaultAsync(c => c.Name.ToLower() == "nước lọc");
                    if (autoCat == null)
                    {
                        var firstKitchen = await _db.Kitchen.FirstOrDefaultAsync();
                        autoCat = new CategoryModel { Name = "Nước lọc", Description = "Auto trigger", isAutoFire = true, Priority = 0, Status = 1, KitchenId = firstKitchen?.Id ?? 0, Slug = "nuoc-loc" };
                        _db.Categories.Add(autoCat);
                        await _db.SaveChangesAsync();
                    }
                    nuocLocProduct = new ProductModel { Name = "Nước lọc", Price = 0, CategoryId = autoCat.Id, Status = 1, Image = "default.jpg", Description = "Auto trigger" };
                    _db.Products.Add(nuocLocProduct);
                    await _db.SaveChangesAsync();
                }

                _db.OrderDetails.Add(new OrderDetailsModel
                {
                    Order = order,
                    ProductId = nuocLocProduct.Id,
                    Quantity = 1,
                    CreateDate = DateTime.Now,
                    Status = StatusProduct.Pending,
                    IsFired = true,
                    FiredAt = DateTime.Now,
                    UnitPrice = 0,
                    Note = "Auto Trigger"
                });
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

            // ── Áp mã giảm giá (nếu có) ──
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
                        coupon.DiscountAmount > order.SubTotal
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
                        coupon.DiscountAmount > order.SubTotal
                        ? order.SubTotal
                        : coupon.DiscountAmount;
                }
            }

            await _db.SaveChangesAsync();

            // ── Xác định phương thức thanh toán ──
            bool isVnPay = paymentMethod == 2;

            string paymentUrl = "";
            string referenceNumber = "";

            if (isVnPay)
            {
                // VNPay: chỉ tạo URL + lưu reference, CHƯA mark Paid
                // (sẽ mark Paid khi nhận IPN callback)
                referenceNumber = order.OrderCode + "_" +
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                long amountVnd = (long)Math.Round(order.GrandTotal);

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                paymentUrl = _vnpPay.CreatePaymentUrl(ipAddress, referenceNumber, amountVnd);

                // Ghi nhận method và reference để đối chiếu IPN
                order.Method = PaymentMethod.VNPay;
                order.VnPayReference = referenceNumber;
                await _db.SaveChangesAsync();

                // Trả về ngay cho Android hiển thị QR (chưa đóng bàn)
                return Ok(new CheckoutResultDto
                {
                    OrderCode = order.OrderCode,
                    TableName = table.TableName,
                    TotalAmount = order.GrandTotal,
                    CheckInTime = order.CreatedDate
                        .ToString("HH:mm dd/MM/yyyy"),
                    CheckOutTime = DateTime.Now
                        .ToString("HH:mm dd/MM/yyyy"),
                    PaymentUrl = paymentUrl,
                    ReferenceNumber = referenceNumber
                });
            }
            else
            {
                // Cash: checkout ngay, mark Paid và giải phóng bàn
                order.Method = PaymentMethod.Cash;
                await _db.SaveChangesAsync();

                var (success, message) =
                    await _orderState.CheckoutTableAsync(tableId);

                if (!success)
                {
                    return BadRequest(new { message });
                }

                return Ok(new CheckoutResultDto
                {
                    OrderCode = order.OrderCode,
                    TableName = table.TableName,
                    TotalAmount = order.GrandTotal,
                    CheckInTime = order.CreatedDate
                        .ToString("HH:mm dd/MM/yyyy"),
                    CheckOutTime = DateTime.Now
                        .ToString("HH:mm dd/MM/yyyy"),
                    PaymentUrl = "",
                    ReferenceNumber = ""
                });
            }
        }
    }
}
