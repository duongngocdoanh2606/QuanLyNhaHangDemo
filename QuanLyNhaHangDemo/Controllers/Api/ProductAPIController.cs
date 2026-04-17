using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Repository;

namespace QuanLyNhaHangDemo.Controllers.Api
{
    [Route("api/[controller]")]
    public class ProductAPIController : Controller
    {
        private readonly DataContext _context; // Database của bạn

        public ProductAPIController(DataContext context) { _context = context; }
        [HttpGet("all")]
        public IActionResult GetProducts(int? categoryId)
        {
            var query = _context.Products.AsQueryable();
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            var products = query
                .Select(p => new
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Description = p.Description,
                    CategoryId = p.CategoryId,
                    ImageUrl = $"{Request.Scheme}://{Request.Host}/media/products/{p.Image}"
                })
                .ToList();

            return Ok(new { success = true, data = products });
        }
    }
}
