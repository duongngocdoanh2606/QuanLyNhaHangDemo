using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Models;
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
            return View(await _dataContext.Products.OrderByDescending(p => p.Id).Include(p => p.Category).ToListAsync());
        }
        [HttpGet]
        public IActionResult Create()
        {
            var activeCategories = _dataContext.Categories
                .Where(c => c.Status == 1)        // chỉ lấy category đang hiện
                .ToList();

            var activeBrands = _dataContext.Brands
                .Where(b => b.Status == 1)        // nếu bạn cũng có Status cho Brand
                .ToList();

            ViewBag.Categories = new SelectList(activeCategories, "Id", "Name");
            ViewBag.Brands = new SelectList(activeBrands, "Id", "Name");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductModel product)
        {
            var activeCategories = _dataContext.Categories
                .Where(c => c.Status == 1)
        .       ToList();

            var activeBrands = _dataContext.Brands
                .Where(b => b.Status == 1)
                .ToList();
            ViewBag.Categories = new SelectList(_dataContext.Categories, "Id", "Name", product.CategoryId);


            if (ModelState.IsValid)
            {

                product.Slug = product.Name.Replace(" ", "-");
                var slug = await _dataContext.Products.FirstOrDefaultAsync(p => p.Slug == product.Slug);
                if (slug != null)
                {
                    ModelState.AddModelError("", "San pham da co trong database");
                    return View(product);
                }


                if (product.ImageUpLoad != null)
                {
                    string upLoadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "media/products");
                    string imageName = Guid.NewGuid().ToString() + "_" + product.ImageUpLoad.FileName;
                    string filePath = Path.Combine(upLoadsDir, imageName);
                    FileStream fs = new FileStream(filePath, FileMode.Create);
                    await product.ImageUpLoad.CopyToAsync(fs);
                    fs.Close();
                    product.Image = imageName;

                }

                _dataContext.Add(product);
                await _dataContext.SaveChangesAsync();
                TempData["success"] = "Them san pham thanh cong";
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
        public async Task<IActionResult> Edit(int Id)
        {
            ProductModel product = await _dataContext.Products.FindAsync(Id);
            var activeCategories = _dataContext.Categories
                .Where(c => c.Status == 1)
                .ToList();

            var activeBrands = _dataContext.Brands
                .Where(b => b.Status == 1)
                .ToList();
            ViewBag.Categories = new SelectList(_dataContext.Categories, "Id", "Name", product.CategoryId);

            return View(product);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductModel product)
        {
            var activeCategories = _dataContext.Categories.Where(c => c.Status == 1).ToList();
            var activeBrands = _dataContext.Brands.Where(b => b.Status == 1).ToList();
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
        [Route("AddQuantity")]
        [HttpGet]
        public async Task<IActionResult> AddQuantity(int Id)
        {

            var product = await _dataContext.ProductQuantities.Where(pq => pq.ProductId == Id).ToListAsync();
            ViewBag.ProductQuantity = product;
            ViewBag.Id = Id;
            return View();
        }
        [Route("UpdateMoreQuantity")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMoreQuantity(ProductQuantityModel model)
        {
            var product = await _dataContext.Products.FindAsync(model.ProductId);
            if (product == null)
            {
                return NotFound();
            }

            // Lấy tất cả nguyên liệu mà product này sử dụng
            var usages = await _dataContext.ProductMaterials
                .Include(pm => pm.Material)
                .Where(pm => pm.ProductId == model.ProductId)
                .ToListAsync();

            if (usages.Any())
            {
                var maxList = new List<decimal>();

                foreach (var usage in usages)
                {
                    var material = usage.Material;

                    // Số lượng nguyên liệu được phép dùng (trên mức ReorderLevel)
                    decimal usable = material.CurrentQuantity - material.ReorderLevel;
                    if (usable <= 0)
                    {
                        maxList.Add(0);
                        continue;
                    }

                    // Số sản phẩm tối đa tạo được từ nguyên liệu này
                    decimal maxFromThisMaterial = usable / usage.QuantityPerProduct;
                    maxList.Add(maxFromThisMaterial);
                }

                // Tổng số sản phẩm TỐI ĐA có thể có (nếu từ 0)
                int maxAllow = (int)Math.Floor(maxList.Min());

                // ⚠ ĐÃ CÓ sẵn bao nhiêu sản phẩm trong kho
                int currentQty = product.Quantity;

                // Số lượng còn được phép thêm nữa
                int remainingAllow = maxAllow - currentQty;
                if (remainingAllow < 0) remainingAllow = 0;

                // Nếu số lượng muốn thêm vượt quá phần còn được phép
                if (model.Quantity > remainingAllow)
                {
                    string msg;
                    if (remainingAllow == 0)
                    {
                        msg = $"Không thể thêm nữa. Tồn kho nguyên liệu chỉ đủ cho {maxAllow} sản phẩm, " +
                              $"hiện tại đã có {currentQty} sản phẩm.";
                    }
                    else
                    {
                        msg = $"Theo tồn kho nguyên liệu, tổng tối đa là {maxAllow} sản phẩm. " +
                              $"Hiện đã có {currentQty}, bạn chỉ được phép thêm tối đa {remainingAllow} sản phẩm nữa.";
                    }

                    ModelState.AddModelError("Quantity", msg);

                    var productQtyList = await _dataContext.ProductQuantities
                        .Where(pq => pq.ProductId == model.ProductId)
                        .ToListAsync();

                    ViewBag.ProductQuantity = productQtyList;
                    ViewBag.Id = model.ProductId;

                    return View("AddQuantity", model);
                }
            }

            // ✅ Nếu qua được IF trên → OK, tiến hành cộng số lượng
            product.Quantity += model.Quantity;
            model.DateCreated = DateTime.Now;

            _dataContext.ProductQuantities.Add(model);
            _dataContext.Products.Update(product);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Cập nhật số lượng sản phẩm thành công";
            return RedirectToAction(nameof(AddQuantity), new { id = model.ProductId });
        }



    }

}
