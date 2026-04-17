using Microsoft.AspNetCore.Mvc;
using QuanLyNhaHangDemo.Models.Dtos;
using QuanLyNhaHangDemo.Repository;

namespace QuanLyNhaHangDemo.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryAPIController : Controller
    {
        private readonly DataContext _context;

        public CategoryAPIController(DataContext context)
        {
            _context = context;
        }
        [HttpGet("all")]
        public IActionResult getCategories()
        {
            var categories = _context.Categories.Select(
                c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name
                }).ToList();
            return Ok(new { success = true, data = categories });
        }
    }
}
