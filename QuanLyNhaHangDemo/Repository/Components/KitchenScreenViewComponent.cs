using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Models;
using QuanLyNhaHangDemo.Repository;

public class KitchenScreenViewComponent : ViewComponent
{
    private readonly DataContext _context;

    public KitchenScreenViewComponent(DataContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync(int kitchenId)
    {
        var items = await _context.OrderDetails
            .Include(o => o.Product)
            .ThenInclude(p => p.Category)
            .Where(o =>
                o.Product.Category.KitchenId == kitchenId &&
                o.Status != StatusProduct.Done &&
                o.Status != StatusProduct.Cancelled)
            .OrderBy(o => o.Product.Category.Priority)
            .ThenBy(o => o.Id)
            .ToListAsync();

        return View(items);
    }
}
