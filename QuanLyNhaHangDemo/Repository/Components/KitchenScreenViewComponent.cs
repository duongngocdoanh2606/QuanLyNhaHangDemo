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
        var filterDate = DateTime.Today;

        var items = await _context.OrderDetails
            .Include(o => o.Product)
                .ThenInclude(p => p.Category)
            .Where(o =>
                o.Product.Category.KitchenId == kitchenId &&
                o.IsFired &&
                o.CreateDate >= filterDate &&
                o.Status != StatusProduct.Done &&
                o.Status != StatusProduct.Served &&
                o.Status != StatusProduct.Cancelled)
            // --- SỬA ĐOẠN SẮP XẾP THEO LUỒNG CŨ TẠI ĐÂY ---
            .OrderByDescending(o => o.FireCount > 1)       // 1. Món nào có FireCount > 1 (đang nấu lại) nhảy lên đầu
            .ThenByDescending(o => o.IsManuallyFired)     // 2. Món bấm tay ép làm trước đứng thứ hai
            .ThenBy(o => o.FiredAt)                        // 3. Món nào được Fire xuống trước làm trước
            .ToListAsync();

        return View(items);
    }
}