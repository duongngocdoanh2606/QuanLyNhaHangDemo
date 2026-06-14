using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Models.Dtos;
using QuanLyNhaHangDemo.Repository;

namespace QuanLyNhaHangDemo.Controllers.Api
{
    [Route("api/[controller]")]
    public class ProductAPIController : Controller
    {
        private readonly DataContext _context;
        private readonly IConfiguration _config;

        public ProductAPIController(DataContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        private string BuildImageUrl(string imageName)
        {
            if (string.IsNullOrWhiteSpace(imageName))
                return string.Empty;

            if (imageName.StartsWith("http://") || imageName.StartsWith("https://"))
                return imageName;

            // ưu tiên lấy BaseUrl từ cấu hình (Railway env var hoặc appsettings)
            var baseUrl = _config["App:BaseUrl"];
            if (string.IsNullOrEmpty(baseUrl))
            {
                // Fallback: dùng X-Forwarded-Proto nếu có (railway proxy)
                var scheme = Request.Headers.ContainsKey("X-Forwarded-Proto")
                    ? Request.Headers["X-Forwarded-Proto"].ToString()
                    : Request.Scheme;
                baseUrl = $"{scheme}://{Request.Host}";
            }
            return $"{baseUrl.TrimEnd('/')}/media/products/{imageName}";
        }
        [HttpGet("all")]
        public IActionResult GetProducts(int? categoryId)
        {
            var query = _context.Products
                .Include(p => p.ProductMaterials)
                    .ThenInclude(pm => pm.Material)
                .Include(p => p.ProductModifierGroups)
                    .ThenInclude(pmg => pmg.ModifierGroup)
                        .ThenInclude(mg => mg.Modifiers)
                            .ThenInclude(m => m.ModifierMaterials)
                                .ThenInclude(mm => mm.Material)
                .AsQueryable();

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            var productEntities = query.ToList();

            var products = productEntities
                .Select(p => 
                {
                    bool isAvailable = true;

                    // Check if base materials are sufficient
                    bool isBaseAvailable = !p.ProductMaterials.Any(pm => (pm.Material.CurrentQuantity - pm.Material.ReorderLevel) < pm.QuantityRequired);
                    if (!isBaseAvailable) 
                    {
                        isAvailable = false;
                    }
                    else 
                    {
                        // Check if it has a Size modifier group
                        var sizeGroup = p.ProductModifierGroups.FirstOrDefault(pmg => pmg.ModifierGroup.Type == "Size");
                        if (sizeGroup != null)
                        {
                            // Check if AT LEAST ONE size is available
                            bool hasAvailableSize = sizeGroup.ModifierGroup.Modifiers.Any(m => 
                                !m.ModifierMaterials.Any(mm => (mm.Material.CurrentQuantity - mm.Material.ReorderLevel) < mm.QuantityRequired));
                            if (!hasAvailableSize) isAvailable = false;
                        }
                    }

                    return new ProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Price = p.Price,
                        Description = p.Description,
                        CategoryId = p.CategoryId,
                        Image = p.Image,
                        ImageUrl = BuildImageUrl(p.Image),
                        IsAvailable = isAvailable
                    };
                })
                .ToList();

            return Ok(new { success = true, data = products });
        }
        [HttpGet("{id}")]
        public IActionResult GetProductDetail(int id)
        {
            var product = _context.Products
                .Include(p => p.ProductMaterials)
                    .ThenInclude(pm => pm.Material)
                .Include(p => p.ProductModifierGroups)
                    .ThenInclude(pmg => pmg.ModifierGroup)
                        .ThenInclude(mg => mg.Modifiers)
                            .ThenInclude(m => m.ModifierMaterials)
                                .ThenInclude(mm => mm.Material)
                .FirstOrDefault(p => p.Id == id);

            if (product == null)
            {
                return NotFound(new { success = false, message = "Sản phẩm không tồn tại" });
            }

            bool isAvailable = true;
            bool isBaseAvailable = !product.ProductMaterials.Any(pm => (pm.Material.CurrentQuantity - pm.Material.ReorderLevel) < pm.QuantityRequired);
            if (!isBaseAvailable) 
            {
                isAvailable = false;
            }
            else 
            {
                var sizeGroup = product.ProductModifierGroups.FirstOrDefault(pmg => pmg.ModifierGroup.Type == "Size");
                if (sizeGroup != null)
                {
                    bool hasAvailableSize = sizeGroup.ModifierGroup.Modifiers.Any(m => 
                        !m.ModifierMaterials.Any(mm => (mm.Material.CurrentQuantity - mm.Material.ReorderLevel) < mm.QuantityRequired));
                    if (!hasAvailableSize) isAvailable = false;
                }
            }

            var result = new ProductDetailDto
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                Description = product.Description,
                CategoryId = product.CategoryId,
                Image = BuildImageUrl(product.Image),
                IsAvailable = isAvailable,

                ModifierGroups = product.ProductModifierGroups.Select(pmg => new ModifierGroupDto
                {
                    Id = pmg.ModifierGroup.Id,
                    Name = pmg.ModifierGroup.Name,

                    // 🚀 SỬA TẠI ĐÂY: Lấy giá trị Type từ tầng ModifierGroup đưa ra API
                    Type = pmg.ModifierGroup.Type,

                    Options = pmg.ModifierGroup.Modifiers.Select(m => new ModifierDto
                    {
                        Id = m.Id,
                        Name = m.Name,
                        ExtraPrice = m.Price,
                        IsAvailable = !m.ModifierMaterials.Any(mm => (mm.Material.CurrentQuantity - mm.Material.ReorderLevel) < mm.QuantityRequired)
                    }).ToList()
                }).ToList()
            };

            return Ok(new { success = true, data = result });
        }
    }
}
