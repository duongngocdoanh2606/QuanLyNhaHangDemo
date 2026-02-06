using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using QuanLyNhaHangDemo.Areas.Admin.Repository;
using QuanLyNhaHangDemo.Models;
using QuanLyNhaHangDemo.Repository;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace QuanLyNhaHangDemo.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly IEmailSender _emailSender;

        public CheckoutController(IEmailSender emailsender, DataContext context)
        {
            _dataContext = context;
            _emailSender = emailsender;
        }

        public async Task<IActionResult> Checkout()
        {
            // 1. Phải đăng nhập
            if (!(User.Identity?.IsAuthenticated ?? false))
            {
                return RedirectToAction("Login", "Account");
            }

            bool isCustomer = User.IsInRole("Customer");

            string userNameForOrder;
            string? userEmail = null;

            if (isCustomer)
            {
                // CUSTOMER: dùng email làm UserName đơn hàng
                userEmail = User.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(userEmail))
                {
                    return RedirectToAction("Login", "Account");
                }
                userNameForOrder = userEmail;
            }
            else
            {
                // WAITER / ADMIN
                userNameForOrder = User.Identity?.Name ?? "internal-user";
            }

            // 2. Bàn hiện tại (nếu order tại quán)
            int? tableId = HttpContext.Session.GetInt32("CurrentTableId");

            OrderModel orderItem;
            bool isNewOrder = false;

            // 3. WAITer + có bàn → cố gắng dùng lại order cũ
            if (!isCustomer && tableId.HasValue)
            {
                orderItem = await _dataContext.Orders
                    .FirstOrDefaultAsync(o =>
                        o.TableId == tableId.Value &&
                        o.Status != 2 &&  
                        o.Status != 5);    

                if (orderItem == null)
                {
                
                    var ordercode = Guid.NewGuid().ToString();

                    orderItem = new OrderModel
                    {
                        OrderCode = ordercode,
                        UserName = userNameForOrder,
                        CreatedDate = DateTime.Now,
                        Status = 0,
                        TableId = tableId,
                        ShippingCost = 0            
                    };

                    _dataContext.Orders.Add(orderItem);
                    await _dataContext.SaveChangesAsync();
                    isNewOrder = true;
                }
            }
            else
            {
                var ordercode = Guid.NewGuid().ToString();
                decimal shippingPrice = 0;

                var shippingPriceCookie = Request.Cookies["ShippingPrice"];
                if (shippingPriceCookie != null)
                {
                    var shippingPriceJson = shippingPriceCookie;
                    shippingPrice = JsonConvert.DeserializeObject<decimal>(shippingPriceJson);
                }

                orderItem = new OrderModel
                {
                    OrderCode = ordercode,
                    UserName = userNameForOrder,
                    CreatedDate = DateTime.Now,
                    Status = 0,

                    ShippingCost = shippingPrice,
                    TableId = null
                };

                _dataContext.Orders.Add(orderItem);
                await _dataContext.SaveChangesAsync();
                isNewOrder = true;
            }

            var orderCode = orderItem.OrderCode;

            // 5. Lấy cart từ session
            List<CartItemModel> cartItems =
                HttpContext.Session.GetJson<List<CartItemModel>>("Cart") ?? new List<CartItemModel>();

            if (!cartItems.Any())
            {
                TempData["error"] = "Giỏ hàng đang trống.";
                if (isCustomer)
                    return RedirectToAction("Index", "Home");
                else
                    return RedirectToAction("Index", "Table");
            }

            // 6. Thêm OrderDetails vào orderCode (cũ hoặc mới)
            foreach (var cart in cartItems)
            {
                var orderDetail = new OrderDetails
                {
                    UserName = userNameForOrder,
                    OrderCode = orderCode,
                    ProductId = cart.ProductId,
                    Quantity = cart.Quantity,
                    Price = cart.Price,
                    Status = 0      // món mới
                };

                var product = await _dataContext.Products
                    .FirstAsync(p => p.Id == cart.ProductId);

                product.Quantity -= cart.Quantity;
                product.Sold += cart.Quantity;

                _dataContext.Products.Update(product);
                _dataContext.OrderDetails.Add(orderDetail);
            }

            await _dataContext.SaveChangesAsync();

            if (!isCustomer && tableId.HasValue && isNewOrder)
            {
                var table = await _dataContext.Table.FindAsync(tableId.Value);
                if (table != null)
                {
                    table.Status = TableStatus.Serving;
                    _dataContext.Table.Update(table);
                    await _dataContext.SaveChangesAsync();
                }
            }

            // 8. Xóa cart khỏi session
            HttpContext.Session.Remove("Cart");

            if (isCustomer && isNewOrder)
            {
                // Link xem hóa đơn online cho khách
                var invoiceUrl = Url.Action(
                    action: "ViewOrder",       
                    controller: "Order",
                    values: new { ordercode = orderCode },
                    protocol: Request.Scheme
                );

                var subject = $"Xác nhận đơn hàng {orderCode}";
                var message = $@"
                    Cảm ơn bạn đã đặt hàng tại hệ thống!

                    Mã đơn hàng: {orderCode}
                    Ngày đặt: {DateTime.Now:dd/MM/yyyy HH:mm}

                    Bạn có thể xem chi tiết hóa đơn tại:
                    {invoiceUrl}

                    Trân trọng.";

                await _emailSender.SendEmailAsync(userEmail!, subject, message);
            }

            // mail nội bộ báo đơn mới (chỉ khi tạo mới, cả online và tại bàn)
            if (isNewOrder)
            {
                var adminSubject = "New Order Received";
                var adminMessage = $"You have received a new order. Order Code: {orderCode}";
                await _emailSender.SendEmailAsync("doanhdkdk@gmail.com", adminSubject, adminMessage);
            }

            TempData["success"] = "Đặt món thành công";

            if (isCustomer)
            {
                return RedirectToAction("History", "Account");   
            }
            else
            {
                return RedirectToAction("Index", "Table");    
            }
        }
    }
}
