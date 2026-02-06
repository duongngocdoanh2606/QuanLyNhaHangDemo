using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Models;
using QuanLyNhaHangDemo.Repository;

namespace QuanLyNhaHangDemo.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class OrderController : Controller
    {
        private readonly DataContext _dataContext;
        public OrderController(DataContext context)
        {
            _dataContext = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var orders = await _dataContext.Orders
                .Where(o => o.Status != 2)               // không hiển thị đơn đã hoàn thành
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            return View(orders);
        }


        [HttpGet]
        [Route("ViewOrder")]
        public async Task<IActionResult> ViewOrder(string ordercode)
        {
            var DetailsOrder = await _dataContext.OrderDetails
                .Include(od => od.Product)
                .Where(od => od.OrderCode == ordercode)
                .ToListAsync();

            var order = await _dataContext.Orders
                .FirstOrDefaultAsync(o => o.OrderCode == ordercode);

            if (order == null)
            {
                return NotFound();
            }

            ViewBag.ShippingCost = order.ShippingCost;
            ViewBag.Status = order.Status;

            // ✅ Thêm danh sách trạng thái
            var statusList = new List<SelectListItem>
            {
                new SelectListItem { Value = "0", Text = "Đơn Hàng Mới" },
                new SelectListItem { Value = "1", Text = "Đang xử lý" },
                new SelectListItem { Value = "2", Text = "Hoàn thành" },

            };
            var selected = statusList.FirstOrDefault(x => x.Value == order.Status.ToString());
            if (selected != null)
            {
                selected.Selected = true;
            }

            ViewBag.StatusList = statusList;

            return View(DetailsOrder);
        }

        [HttpPost]
        [Route("UpdateOrder")]
        public async Task<IActionResult> UpdateOrder(string ordercode, int status)
        {
            if (string.IsNullOrEmpty(ordercode))
                return Json(new { success = false, message = "Mã đơn hàng rỗng." });

            var order = await _dataContext.Orders
                .FirstOrDefaultAsync(o => o.OrderCode == ordercode);

            if (order == null)
                return Json(new { success = false, message = "Không tìm thấy đơn hàng." });

            int oldStatus = order.Status;
            order.Status = status;
            _dataContext.Orders.Update(order);

            try
            {
                // CHỈ ghi báo cáo khi chuyển sang Hoàn thành (2) và trước đó chưa hoàn thành
                if (status == 2 && oldStatus != 2)
                {
                    var detailsOrder = await _dataContext.OrderDetails
                        .Where(od => od.OrderCode == ordercode)
                        .ToListAsync();

                    if (detailsOrder.Any())
                    {
                        // 1. Doanh thu: dùng giá bán thực tế trong OrderDetails
                        decimal totalRevenue = detailsOrder.Sum(od => od.Price * od.Quantity);

                        // Nếu muốn tính cả phí ship vào doanh thu thì + thêm:
                        // totalRevenue += order.ShippingCost;

                        // 2. Chi phí nguyên liệu
                        decimal totalMaterialCost = 0m;

                        foreach (var item in detailsOrder)
                        {
                            var usages = await _dataContext.ProductMaterials
                                .Where(pm => pm.ProductId == item.ProductId)
                                .ToListAsync();

                            foreach (var usage in usages)
                            {
                                decimal usedQuantity = item.Quantity * usage.QuantityPerProduct;

                                var lastImport = await _dataContext.InventoryTransactions
                                    .Where(t => t.MaterialId == usage.MaterialId && t.Type == "IN")
                                    .OrderByDescending(t => t.DateCreated)
                                    .ThenByDescending(t => t.Id)
                                    .FirstOrDefaultAsync();

                                decimal unitCost = lastImport?.UnitPrice ?? 0m;
                                totalMaterialCost += usedQuantity * unitCost;
                            }
                        }

                        // 3. Lợi nhuận = Doanh thu – Chi phí nguyên liệu
                        decimal profit = totalRevenue - totalMaterialCost;

                        // 4. Ghi vào bảng Statisticals theo ngày tạo đơn
                        var statisticalModel = await _dataContext.Statisticals
                            .FirstOrDefaultAsync(s => s.DateCreated.Date == order.CreatedDate.Date);

                        int totalQuantity = detailsOrder.Sum(d => d.Quantity);

                        if (statisticalModel != null)
                        {
                            statisticalModel.Sold += 1;
                            statisticalModel.Quantity += totalQuantity;
                            statisticalModel.Revenue += totalRevenue;
                            statisticalModel.Profit += profit;
                            _dataContext.Statisticals.Update(statisticalModel);
                        }
                        else
                        {
                            statisticalModel = new StatisticalModel
                            {
                                DateCreated = order.CreatedDate.Date,
                                Sold = 1,
                                Quantity = totalQuantity,
                                Revenue = totalRevenue,
                                Profit = profit
                            };
                            await _dataContext.Statisticals.AddAsync(statisticalModel);
                        }
                    }

                    if (order.TableId.HasValue)
                    {
                        var tableId = order.TableId.Value;

                        var reservation = await _dataContext.Reservations
                            .Where(r =>
                                r.TableId == tableId &&
                                r.Status == ReservationStatus.Approved // hoặc Pending/Approved tuỳ flow
                            )
                            .OrderByDescending(r => r.ReserveTime)
                            .FirstOrDefaultAsync();

                        if (reservation != null)
                        {
                            reservation.Status = ReservationStatus.Completed;
                            _dataContext.Reservations.Update(reservation);
                        }
                    }

                }

                await _dataContext.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> CompletedOrders(DateTime? from, DateTime? to)
        {
            var ordersQuery = _dataContext.Orders
                .Where(o => o.Status == 2)  // chỉ đơn đã hoàn thành
                .AsQueryable();

            if (from.HasValue)
                ordersQuery = ordersQuery.Where(o => o.CreatedDate.Date >= from.Value.Date);

            if (to.HasValue)
                ordersQuery = ordersQuery.Where(o => o.CreatedDate.Date <= to.Value.Date);

            var list = await ordersQuery
                .OrderByDescending(o => o.CreatedDate)
                .Select(o => new CompletedOrderViewModel
                {
                    OrderCode = o.OrderCode,
                    UserName = o.UserName,
                    CreatedDate = o.CreatedDate,
                    ShippingCost = o.ShippingCost,
                    OrderRevenue = _dataContext.OrderDetails
                        .Where(od => od.OrderCode == o.OrderCode)
                        .Sum(od => od.Price * od.Quantity)
                })
                .ToListAsync();

            // Tổng doanh thu (chỉ tiền món)
            var totalRevenue = list.Sum(x => x.OrderRevenue);

            // Nếu muốn tổng cả ship:
            var totalRevenueWithShipping = list.Sum(x => x.TotalWithShipping);

            ViewBag.From = from;
            ViewBag.To = to;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalRevenueWithShipping = totalRevenueWithShipping;

            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Invoice(string ordercode)
        {
            if (string.IsNullOrEmpty(ordercode))
                return NotFound();

            var order = await _dataContext.Orders
                .FirstOrDefaultAsync(o => o.OrderCode == ordercode);

            if (order == null)
                return NotFound();

            var details = await _dataContext.OrderDetails
                .Include(d => d.Product)
                .Where(d => d.OrderCode == ordercode)
                .ToListAsync();

            decimal total = details.Sum(d => d.Quantity * d.Price);

            // ===== Thêm phần lấy thông tin bàn / online =====
            string tableInfo;
            if (order.TableId.HasValue)
            {
                var table = await _dataContext.Table
                    .FirstOrDefaultAsync(t => t.Id == order.TableId.Value);

                // Nếu có bàn → in số bàn
                if (table != null)
                {
                    // tuỳ thuộc class TableModel của bạn:
                    // ví dụ: table.TableNumber hoặc table.Name
                    tableInfo = $"Bàn số: {table.TableName}";
                }
                else
                {
                    tableInfo = "Đặt tại quán (bàn không xác định)";
                }
            }
            else
            {
                // Không có TableId → đơn online
                tableInfo = "Đặt Online";
            }

            ViewBag.Order = order;
            ViewBag.Total = total;
            ViewBag.Shipping = order.ShippingCost;
            ViewBag.GrandTotal = total + order.ShippingCost;
            ViewBag.Buyer = order.UserName;
            ViewBag.OrderCode = ordercode;
            ViewBag.TableInfo = tableInfo;     // ⭐ truyền sang View

            return View(details);
        }




        [HttpGet]
        [Route("Delete")]
        public async Task<IActionResult> Delete(string ordercode)
        {
            if (string.IsNullOrEmpty(ordercode))
            {
                return NotFound();
            }

            var order = await _dataContext.Orders.FirstOrDefaultAsync(o => o.OrderCode == ordercode);
            if (order == null)
            {
                return NotFound();
            }

            try
            {
                // delete related OrderDetails first (because there is no FK cascade configured)
                var details = await _dataContext.OrderDetails
                                    .Where(d => d.OrderCode == ordercode)
                                    .ToListAsync();

                if (details.Any())
                {
                    _dataContext.OrderDetails.RemoveRange(details);
                }

                // then delete the order
                _dataContext.Orders.Remove(order);

                await _dataContext.SaveChangesAsync();

                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while deleting the order.");
            }
        }
    }
}