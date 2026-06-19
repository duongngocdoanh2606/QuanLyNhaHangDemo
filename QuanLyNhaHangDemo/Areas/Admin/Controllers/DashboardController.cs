using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Models;
using QuanLyNhaHangDemo.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyNhaHangDemo.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")]
    public class DashboardController : Controller
    {
        private readonly DataContext _dataContext;

        public DashboardController(DataContext dataContext)
        {
            _dataContext = dataContext;
        }
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> GetChartData(string type, DateTime? startDate, DateTime? endDate)
        {
            var today = DateTime.Today;
            DateTime? from = null;
            DateTime? to = null;

            // 1. Phân loại khoảng thời gian lọc dữ liệu
            switch (type)
            {
                case "week":
                    from = today.AddDays(-6);
                    to = today;
                    break;
                case "month":
                    from = new DateTime(today.Year, today.Month, 1);
                    to = today;
                    break;
                case "year":
                    from = new DateTime(today.Year, 1, 1);
                    to = today;
                    break;
                case "custom":
                    if (startDate.HasValue && endDate.HasValue && startDate <= endDate)
                    {
                        from = startDate.Value.Date;
                        to = endDate.Value.Date;
                    }
                    break;
                case "all":
                default:
                    break;
            }

            // 2. Lấy danh sách hóa đơn đã thanh toán
            var ordersQuery = _dataContext.Orders
                .Where(o => o.Status == OrderModel.OrderStatus.Paid);

            if (from.HasValue)
                ordersQuery = ordersQuery.Where(o => o.CreatedDate.Date >= from.Value.Date);
            if (to.HasValue)
                ordersQuery = ordersQuery.Where(o => o.CreatedDate.Date <= to.Value.Date);

            var ordersList = await ordersQuery.ToListAsync();
            var orderIds = ordersList.Select(o => o.Id).ToList();

            // 3. Lấy chi phí xuất kho theo đơn — tách riêng OUT_SALE và OUT_REMAKE
            var outTransactions = await _dataContext.InventoryTransactions
                .Where(t => t.Type == "OUT" && t.OrderId.HasValue && orderIds.Contains(t.OrderId.Value))
                .ToListAsync();

            // Chi phí nguyên liệu gốc (lần đầu chế biến)
            var saleCostsDict = outTransactions
                .Where(t => t.Reason == "OUT_SALE")
                .GroupBy(t => t.OrderId.Value)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalPrice));

            // Chi phí làm lại món (remake)
            var remakeCostsDict = outTransactions
                .Where(t => t.Reason == "OUT_REMAKE")
                .GroupBy(t => t.OrderId.Value)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalPrice));

            // 4. Gom nhóm dữ liệu theo ngày để vẽ biểu đồ
            var chartDataQuery = ordersList
                .GroupBy(o => o.CreatedDate.Date)
                .Select(g =>
                {
                    decimal dayRevenue = g.Sum(o => o.GrandTotal);

                    decimal daySaleCost = g.Sum(o =>
                        saleCostsDict.ContainsKey(o.Id) ? saleCostsDict[o.Id] : 0m);

                    decimal dayRemakeCost = g.Sum(o =>
                        remakeCostsDict.ContainsKey(o.Id) ? remakeCostsDict[o.Id] : 0m);

                    decimal dayProfit = dayRevenue - daySaleCost - dayRemakeCost;

                    return new
                    {
                        date = g.Key.ToString("yyyy-MM-dd"),
                        revenue = dayRevenue,
                        profit = dayProfit
                    };
                })
                .OrderBy(x => x.date)
                .ToList();

            // 5. Tính toán tổng cộng cho các thẻ hiển thị trên Dashboard
            decimal totalSubRevenue  = ordersList.Sum(o => o.SubTotal);
            decimal totalVAT         = ordersList.Sum(o => o.VATAmount);
            decimal totalService     = ordersList.Sum(o => o.ServiceAmount);
            decimal totalDiscount    = ordersList.Sum(o => o.DiscountAmount);
            decimal totalRevenue     = ordersList.Sum(o => o.GrandTotal);   // Thực tế thu từ khách

            decimal totalSaleCost    = outTransactions.Where(t => t.Reason == "OUT_SALE").Sum(t => t.TotalPrice);
            decimal totalRemakeCost  = outTransactions.Where(t => t.Reason == "OUT_REMAKE").Sum(t => t.TotalPrice);
            decimal totalProfit      = totalRevenue - totalSaleCost - totalRemakeCost;

            // 6. Tính toán Top bán chạy, Top bán ế, Khung giờ cao điểm
            var orderDetailsQuery = _dataContext.OrderDetails
                .Include(od => od.Product)
                .Where(od => orderIds.Contains(od.OrderId) && od.Status != StatusProduct.Cancelled);

            var productSales = await orderDetailsQuery
                .GroupBy(od => new { od.ProductId, od.Product.Name })
                .Select(g => new
                {
                    productName = g.Key.Name,
                    quantity = g.Sum(x => x.Quantity)
                })
                .ToListAsync();

            var topSelling = productSales
                .OrderByDescending(x => x.quantity)
                .Take(5)
                .ToList();

            var leastSelling = productSales
                .OrderBy(x => x.quantity)
                .Take(5)
                .ToList();

            var peakHourGroup = ordersList
                .GroupBy(o => o.CreatedDate.Hour)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            var peakHour = peakHourGroup != null 
                ? new { hour = peakHourGroup.Key, orderCount = peakHourGroup.Count() } 
                : null;

            // 7. Trả về dữ liệu cho Frontend
            return Json(new
            {
                chartData = chartDataQuery,
                summary = new
                {
                    totalSubRevenue,        // Doanh thu món gốc (SubTotal)
                    totalVAT,               // Thuế VAT
                    totalService,           // Phí dịch vụ
                    totalDiscount,          // Tổng giảm giá
                    totalRevenue,           // Doanh thu thực tế (GrandTotal)
                    totalSaleCost,          // Chi phí nguyên liệu (OUT_SALE)
                    totalRemakeCost,        // Chi phí làm lại (OUT_REMAKE)
                    totalProfit             // Lợi nhuận thực = GrandTotal − OUT_SALE − OUT_REMAKE
                },
                insights = new
                {
                    topSelling,
                    leastSelling,
                    peakHour
                }
            });
        }
    }
}