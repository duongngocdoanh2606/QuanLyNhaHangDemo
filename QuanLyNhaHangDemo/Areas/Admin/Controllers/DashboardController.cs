using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Thêm nếu chưa có
using QuanLyNhaHangDemo.Repository;
using System;
using System.Collections.Generic;
using System.Linq; // Thêm nếu chưa có
using System.Threading.Tasks;

namespace QuanLyNhaHangDemo.Areas.Admin.Controllers
{
    [Area("Admin")]
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

            // 2. Lấy danh sách hóa đơn đã thanh toán (Android bấm)
            var ordersQuery = _dataContext.Orders
                .Where(o => o.Status == OrderModel.OrderStatus.Paid);

            if (from.HasValue)
                ordersQuery = ordersQuery.Where(o => o.CreatedDate.Date >= from.Value.Date);
            if (to.HasValue)
                ordersQuery = ordersQuery.Where(o => o.CreatedDate.Date <= to.Value.Date);

            var ordersList = await ordersQuery.ToListAsync();
            var orderIds = ordersList.Select(o => o.Id).ToList();

            // 3. Lấy chi phí xuất kho tương ứng (Bếp bấm hoàn thành món)
            var outTransactions = await _dataContext.InventoryTransactions
                .Where(t => t.Type == "OUT" && t.OrderId.HasValue && orderIds.Contains(t.OrderId.Value))
                .ToListAsync();

            // Gom tổng chi phí gốc theo từng OrderId
            var orderCostsDict = outTransactions
                .GroupBy(t => t.OrderId.Value)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalPrice));

            // 4. Gom nhóm dữ liệu theo Ngày để vẽ biểu đồ đường/cột (Đã sửa lỗi Lambda)
            var chartDataQuery = ordersList
                .GroupBy(o => o.CreatedDate.Date)
                .Select(g => {
                    decimal dayRevenue = g.Sum(o => o.GrandTotal);

                    // Sửa lỗi: Tính toán tường minh chi phí trong ngày của nhóm bàn/đơn này
                    decimal dayCost = g.Sum(o => {
                        return orderCostsDict.ContainsKey(o.Id) ? orderCostsDict[o.Id] : 0m;
                    });

                    decimal dayProfit = dayRevenue - dayCost;

                    return new
                    {
                        date = g.Key.ToString("yyyy-MM-dd"),
                        revenue = dayRevenue,
                        profit = dayProfit
                    };
                })
                .OrderBy(x => x.date)
                .ToList();

            // 5. Tính toán Tổng cộng cho các thẻ hiển thị trên đầu Dashboard
            decimal totalRevenue = ordersList.Sum(o => o.GrandTotal);
            decimal totalCost = outTransactions.Sum(t => t.TotalPrice);
            decimal totalProfit = totalRevenue - totalCost;

            // 6. Trả về đúng dữ liệu cốt lõi cho Frontend của bạn
            return Json(new
            {
                chartData = chartDataQuery,
                summary = new
                {
                    totalRevenue = totalRevenue,
                    totalProfit = totalProfit
                }
            });
        }
    }
}