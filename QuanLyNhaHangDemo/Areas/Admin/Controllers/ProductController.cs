using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Models;
using QuanLyNhaHangDemo.Models.Dtos;
using QuanLyNhaHangDemo.Repository;

namespace QuanLyNhaHangDemo.Areas.Admin.Controllers
{

    [Area("Admin")]
    [Authorize]
    public class ProductController : Controller
    {

        private readonly DataContext _dataContext;
        private readonly IWebHostEnvironment _webHostEnvironment;


        public ProductController(DataContext context, IWebHostEnvironment webHostEnvironment)
        {
            _dataContext = context;
            _webHostEnvironment = webHostEnvironment;
        }
        public async Task<IActionResult> Index()
        {
            var products = await _dataContext.Products
                .Include(p => p.Category)
                .Include(p => p.ProductModifierGroups)
                    .ThenInclude(pmg => pmg.ModifierGroup) // 🔥 QUAN TRỌNG
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            ViewBag.AllModifierGroups = await _dataContext.ModifierGroups.ToListAsync();

            return View(products);
        }
        [HttpGet]
        public IActionResult Create()
        {
            var activeCategories = _dataContext.Categories.Where(c => c.Status == 1).ToList();
            ViewBag.Categories = new SelectList(activeCategories, "Id", "Name");

            // Lấy danh sách các nhóm Modifier để Admin chọn gán cho sản phẩm
            ViewBag.ModifierGroups = _dataContext.ModifierGroups.ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductModel product, int[] selectedGroups)
        {
            // Reload lại ViewBag nếu Model bị lỗi để không hỏng giao diện
            ViewBag.Categories = new SelectList(_dataContext.Categories.Where(c => c.Status == 1), "Id", "Name", product.CategoryId);
            ViewBag.ModifierGroups = _dataContext.ModifierGroups.ToList();

            if (ModelState.IsValid)
            {
                // Logic Slug
                product.Slug = product.Name.Replace(" ", "-").ToLower();
                var slugExists = await _dataContext.Products.AnyAsync(p => p.Slug == product.Slug);
                if (slugExists)
                {
                    ModelState.AddModelError("", "Sản phẩm đã có trong database");
                    return View(product);
                }

                // Logic Image (Giữ nguyên của bạn)
                if (product.ImageUpLoad != null)
                {
                    string upLoadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "media/products");
                    string imageName = Guid.NewGuid().ToString() + "_" + product.ImageUpLoad.FileName;
                    string filePath = Path.Combine(upLoadsDir, imageName);
                    using (var fs = new FileStream(filePath, FileMode.Create))
                    {
                        await product.ImageUpLoad.CopyToAsync(fs);
                    }
                    product.Image = imageName;
                }

                _dataContext.Add(product);
                await _dataContext.SaveChangesAsync(); // Lưu để lấy ProductId

                // BƯỚC MỚI: Gán các Modifier Groups vào sản phẩm
                if (selectedGroups != null && selectedGroups.Length > 0)
                {
                    foreach (var groupId in selectedGroups)
                    {
                        var mapping = new ProModifierGroupModel
                        {
                            ProductId = product.Id,
                            ModifierGroupId = groupId
                        };
                        _dataContext.ProductModifierMappings.Add(mapping);
                    }
                    await _dataContext.SaveChangesAsync();
                }

                TempData["success"] = "Thêm sản phẩm thành công!";
                return RedirectToAction("Index");
            }

            return View(product);
        }
        public async Task<IActionResult> Edit(int Id)
        {
            ProductModel product = await _dataContext.Products.FindAsync(Id);
            var activeCategories = _dataContext.Categories
                .Where(c => c.Status == 1)
                .ToList();
            ViewBag.Categories = new SelectList(_dataContext.Categories, "Id", "Name", product.CategoryId);

            return View(product);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductModel product)
        {
            var activeCategories = _dataContext.Categories.Where(c => c.Status == 1).ToList();
            ViewBag.Categories = new SelectList(_dataContext.Categories, "Id", "Name", product.CategoryId);
            var existed_product = _dataContext.Products.Find(product.Id);
            if (ModelState.IsValid)
            {

                product.Slug = product.Name.Replace(" ", "-");
                if (product.ImageUpLoad != null)
                {
                    string upLoadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "media/products");
                    string imageName = Guid.NewGuid().ToString() + "_" + product.ImageUpLoad.FileName;
                    string filePath = Path.Combine(upLoadsDir, imageName);
                    string oldfileImage = Path.Combine(upLoadsDir, existed_product.Image);
                    try
                    {
                        if (System.IO.File.Exists(oldfileImage))
                        {
                            System.IO.File.Delete(oldfileImage);
                        }
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", "An error");
                    }
                    FileStream fs = new FileStream(filePath, FileMode.Create);
                    await product.ImageUpLoad.CopyToAsync(fs);
                    fs.Close();
                    existed_product.Image = imageName;
                }

                existed_product.Name = product.Name;
                existed_product.Description = product.Description;
                existed_product.Price = product.Price;
                existed_product.CategoryId = product.CategoryId;

                _dataContext.Update(existed_product);
                await _dataContext.SaveChangesAsync();
                TempData["success"] = "Cap nhat san pham thanh cong";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["error"] = "Model co mot vai thu dang bi loi";
                List<string> errors = new List<string>();
                foreach (var value in ModelState.Values)
                {
                    foreach (var error in value.Errors)
                    {
                        errors.Add(error.ErrorMessage);
                    }
                }
                string errorMessage = string.Join("\n", errors);
                return BadRequest(errorMessage);
            }

            return View(product);
        }
        public async Task<IActionResult> Delete(int Id)
        {
            ProductModel product = await _dataContext.Products.FindAsync(Id);
            if (!string.Equals(product.Image, "noname.jpg"))
            {
                string upLoadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "media/products");
                string oldfileImage = Path.Combine(upLoadsDir, product.Image);
                try
                {
                    if (System.IO.File.Exists(oldfileImage))
                    {
                        System.IO.File.Delete(oldfileImage);
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error");
                }
            }
            _dataContext.Products.Remove(product);
            await _dataContext.SaveChangesAsync();
            TempData["success"] = "Sản phẩm đã xóa";
            return RedirectToAction("Index");
        }
        [Route("AddQuantity/{id}")]
        [HttpGet]
        public async Task<IActionResult> AddQuantity(int id)
        {
            // 1. Lấy thông tin sản phẩm
            var product = await _dataContext.Products.FindAsync(id);
            if (product == null)
            {
                TempData["error"] = "Không tìm thấy sản phẩm.";
                return RedirectToAction("Index");
            }

            // 2. Lấy lịch sử hoặc danh sách các lần nạp số lượng trước đó của món này
            var quantities = await _dataContext.ProductQuantities
                .Where(pq => pq.ProductId == id)
                .OrderByDescending(pq => pq.Id)
                .ToListAsync();

            ViewBag.Product = product;
            ViewBag.ProductQuantity = quantities;
            ViewBag.Id = id;

            return View();
        }

        [HttpPost]
        [Route("AddQuantity/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuantity(int id, int quantityToAdd)
        {
            var product = await _dataContext.Products.FindAsync(id);
            if (product == null) return NotFound();

            if (quantityToAdd <= 0)
            {
                TempData["error"] = "Số lượng thêm vào phải lớn hơn 0.";
                return RedirectToAction("AddQuantity", new { id = id });
            }

            // 1. Lấy công thức định mức nguyên liệu (Recipe) của món ăn này
            var recipe = await _dataContext.productMaterials
                .Include(pm => pm.Material)
                .Where(pm => pm.ProductId == id)
                .ToListAsync();

            if (!recipe.Any())
            {
                TempData["error"] = "Món ăn này chưa được cấu hình định mức nguyên liệu! Hãy cấu hình định mức trước.";
                return RedirectToAction("AddMaterial", new { id = id });
            }

            // 2. VÒNG LẶP 1: KIỂM TRA ĐIỀU KIỆN KHO (Chặn nếu chạm hoặc dưới ngưỡng cảnh báo)
            foreach (var item in recipe)
            {
                var material = item.Material;
                if (material == null) continue;

                // Tính tổng lượng nguyên liệu tiêu hao cho số lượng món định thêm
                decimal totalRequired = item.QuantityRequired * quantityToAdd;

                // Dự toán số lượng tồn kho còn lại sau khi làm món
                // Sử dụng chính xác 'CurrentQuantity' từ MaterialModel của bạn
                decimal remainingStock = material.CurrentQuantity - totalRequired;

                // Sử dụng chính xác 'ReorderLevel' làm ngưỡng cảnh báo từ MaterialModel của bạn
                decimal alertThreshold = material.ReorderLevel;

                // Điều kiện chặn: Số lượng còn lại thấp hơn hoặc bằng ngưỡng cảnh báo
                if (remainingStock <= alertThreshold)
                {
                    TempData["error"] = $"Không thể thêm số lượng! Nguyên liệu '{material.Name}' trong kho sau khi làm sẽ giảm xuống còn {remainingStock} {material.Unit}, chạm hoặc dưới mức cảnh báo ({alertThreshold} {material.Unit}).";

                    // Reload dữ liệu để trả về giao diện nạp số lượng
                    ViewBag.Product = product;
                    ViewBag.ProductQuantity = await _dataContext.ProductQuantities.Where(pq => pq.ProductId == id).ToListAsync();
                    ViewBag.Id = id;
                    return RedirectToAction("AddQuantity", new { id = id });
                }
            }

            // 3. VÒNG LẶP 2: THỰC HIỆN TRỪ KHO (Khi mọi điều kiện đều thỏa mãn)
            foreach (var item in recipe)
            {
                decimal totalRequired = item.QuantityRequired * quantityToAdd;

                // Trừ trực tiếp vào lượng tồn kho thực tế
                item.Material.CurrentQuantity -= totalRequired;
                _dataContext.Update(item.Material);
            }

            // 4. Lưu lịch sử nạp vào bảng ProductQuantities
            var productQuantity = new ProductQuantityModel
            {
                ProductId = id,
                Quantity = quantityToAdd,
                // Thêm trường thời gian nếu Model của bạn có (ví dụ: CreatedAt = DateTime.Now)
            };
            _dataContext.ProductQuantities.Add(productQuantity);

            // 5. Cập nhật số lượng món ăn và đồng bộ Trạng thái hoạt động
            // Giả định bạn đang dùng trường 'Sold' để quản lý số lượng món có sẵn trong kho
            product.Sold += quantityToAdd;

            // Điều kiện: Nếu số lượng món nhỏ hơn hoặc bằng 0 thì tự động tắt trạng thái hoạt động
            if (product.Sold <= 0)
            {
                product.Status = 0; // 0: Không hoạt động / Hết hàng
            }
            else
            {
                product.Status = 1; // 1: Hoạt động / Còn hàng
            }

            _dataContext.Products.Update(product);

            // 6. Thực thi cập nhật đồng bộ xuống cơ sở dữ liệu
            await _dataContext.SaveChangesAsync();

            TempData["success"] = $"Đã nạp thêm {quantityToAdd} phần cho món '{product.Name}' thành công và cập nhật kho nguyên liệu!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleModifier([FromBody] ToggleDto dto)
        {
            if (dto == null)
                return BadRequest();

            var existing = await _dataContext.ProductModifierMappings
                .FirstOrDefaultAsync(x =>
                    x.ProductId == dto.ProductId &&
                    x.ModifierGroupId == dto.GroupId);

            if (dto.IsChecked)
            {
                // GẮN
                if (existing == null)
                {
                    _dataContext.ProductModifierMappings.Add(new ProModifierGroupModel
                    {
                        ProductId = dto.ProductId,
                        ModifierGroupId = dto.GroupId
                    });
                }
            }
            else
            {
                if (existing != null)
                {
                    _dataContext.ProductModifierMappings.Remove(existing);
                }
            }

            await _dataContext.SaveChangesAsync();

            return Ok(new { success = true });
        }
        // =================================================================
        // CHỨC NĂNG: ĐỊNH MỨC NGUYÊN LIỆU CHO SẢN PHẨM (ADD MATERIAL)
        // =================================================================

        [HttpGet]
        [Route("AddMaterial/{id}")]
        public async Task<IActionResult> AddMaterial(int id)
        {
            // 1. Tìm thông tin sản phẩm xem có tồn tại không
            var product = await _dataContext.Products.FindAsync(id);
            if (product == null)
            {
                TempData["error"] = "Không tìm thấy sản phẩm yêu cầu.";
                return RedirectToAction("Index");
            }

            // 2. Lấy danh sách định mức nguyên liệu hiện tại của sản phẩm này
            var currentMaterials = await _dataContext.productMaterials
                .Include(pm => pm.Material)
                .Where(pm => pm.ProductId == id)
                .ToListAsync();

            // 3. Lấy toàn bộ danh sách nguyên liệu thô trong kho để hiển thị ở SelectList (Dropdown)
            var allMaterials = await _dataContext.Materials.ToListAsync();

            // Đưa dữ liệu sang giao diện View thông qua ViewBag
            ViewBag.Product = product;
            ViewBag.CurrentMaterials = currentMaterials;
            ViewBag.AllMaterials = new SelectList(allMaterials, "Id", "Name"); // Giả định MaterialModel có Id và Name

            return View();
        }

        [HttpPost]
        [Route("AddMaterial/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMaterial(int id, int[] materialIds, decimal[] quantities)
        {
            var product = await _dataContext.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // 1. Xóa toàn bộ cấu hình định mức nguyên liệu cũ của sản phẩm này để ghi đè dữ liệu mới
            var existingMappings = _dataContext.productMaterials.Where(pm => pm.ProductId == id);
            _dataContext.productMaterials.RemoveRange(existingMappings);

            // 2. Tiến hành gán danh sách nguyên liệu mới nếu có dữ liệu gửi lên
            if (materialIds != null && materialIds.Length > 0)
            {
                for (int i = 0; i < materialIds.Length; i++)
                {
                    // Bỏ qua nếu dòng đó bị lỗi hoặc Admin nhập số lượng <= 0
                    if (materialIds[i] <= 0 || quantities[i] <= 0) continue;

                    var newRecipe = new ProductMaterialsModel
                    {
                        ProductId = id,
                        MaterialId = materialIds[i],
                        QuantityRequired = quantities[i]
                    };

                    _dataContext.productMaterials.Add(newRecipe);
                }
            }

            // 3. Lưu toàn bộ thay đổi vào Database
            await _dataContext.SaveChangesAsync();

            TempData["success"] = $"Cập nhật định mức nguyên liệu cho món '{product.Name}' thành công!";
            return RedirectToAction("Index");
        }
    }

}
