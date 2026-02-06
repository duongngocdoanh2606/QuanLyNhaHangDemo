using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Models;
using QuanLyNhaHangDemo.Repository;

namespace QuanLyNhaHangDemo.Controllers
{
    public class CategoryController : Controller
    {
        private readonly DataContext _dataContext;

        public CategoryController(DataContext context)
        {
            _dataContext = context;
        }

        public async Task<IActionResult> Index(string Slug = "", string sort_by = "",decimal? startprice=null,decimal? endprice=null)
        {
            var category = await _dataContext.Categories
                .FirstOrDefaultAsync(c => c.Slug == Slug && c.Status == 1);

            if (category == null)
                return RedirectToAction("Index", "Home");

            IQueryable<ProductModel> productsByCategory = _dataContext.Products
                .Include(p => p.Category)
                .Where(p => p.CategoryId == category.Id);
            if (startprice.HasValue && endprice.HasValue && startprice <= endprice)
            {
                productsByCategory = productsByCategory
                    .Where(p => p.Price >= startprice.Value && p.Price <= endprice.Value);
            }

            switch (sort_by)
            {
                case "price_increase":               // Giá thấp → cao
                    productsByCategory = productsByCategory.OrderBy(p => p.Price);
                    break;

                case "price_decrease":               // Giá cao → thấp
                    productsByCategory = productsByCategory.OrderByDescending(p => p.Price);
                    break;

                case "price_oldest":                 // Cũ nhất
                    productsByCategory = productsByCategory.OrderBy(p => p.Id);
                    break;

                case "price_newest":                 // Mới nhất
                default:
                    productsByCategory = productsByCategory.OrderByDescending(p => p.Id);
                    break;

                

            }


            var model = await productsByCategory.ToListAsync();
            return View(model);
        }

    }
}
