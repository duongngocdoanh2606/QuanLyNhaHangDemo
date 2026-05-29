using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Models;
using QuanLyNhaHangDemo.Repository;
using System;
using System.Linq;
using System.Threading.Tasks;

public class KitchenScreenViewComponent : ViewComponent
{
    private readonly DataContext _context;

    public KitchenScreenViewComponent(DataContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync(int kitchenId)
    {
        // ĐỒNG BỘ: Chỉ lấy các món ăn từ ngày hôm nay để tránh rác tồn đọng
        var filterDate = DateTime.Today;

        var items = await _context.OrderDetails
            .Include(o => o.Product)
                .ThenInclude(p => p.Category)
            .Where(o =>
                o.Product.Category.KitchenId == kitchenId &&
                o.IsFired &&
                o.CreateDate >= filterDate && // <-- THÊM DÒNG NÀY ĐỂ ĐỒNG BỘ CHẶN RÁC
                o.Status != StatusProduct.Done &&
                o.Status != StatusProduct.Served &&
                o.Status != StatusProduct.Cancelled)
            .OrderByDescending(o => o.FiredAt)
            .ThenBy(o => o.Id)
            .ToListAsync();

        return View(items);
    }
}