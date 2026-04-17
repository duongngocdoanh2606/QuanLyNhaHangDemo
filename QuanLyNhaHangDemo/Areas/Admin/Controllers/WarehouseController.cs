using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Models;
using QuanLyNhaHangDemo.Models.ViewModels;
using QuanLyNhaHangDemo.Repository;

namespace QuanLyNhaHangDemo.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class WarehouseController : Controller
    {
        private readonly DataContext _dataContext;

        public WarehouseController(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        // ENUM thay cho string
        public enum TransactionType
        {
            IN,
            OUT
        }

        // =========================
        // 1. DANH SÁCH NGUYÊN LIỆU
        // =========================
        public async Task<IActionResult> Index()
        {
            var materials = await _dataContext.Materials
                .Include(m => m.Supplier)
                .OrderBy(m => m.Name)
                .ToListAsync();

            // Lấy giá nhập mới nhất (tối ưu)
            var latestPriceByMaterial = await _dataContext.InventoryTransactions
                .Where(t => t.Type == TransactionType.IN.ToString())
                .GroupBy(t => t.MaterialId)
                .Select(g => g.OrderByDescending(x => x.DateCreated)
                              .Select(x => new { x.MaterialId, x.UnitPrice })
                              .FirstOrDefault())
                .ToDictionaryAsync(x => x.MaterialId, x => x.UnitPrice);

            ViewBag.LatestPriceByMaterial = latestPriceByMaterial;

            return View(materials);
        }

        // =========================
        // 2. TẠO NGUYÊN LIỆU
        // =========================
        [HttpGet]
        public IActionResult CreateMaterial()
        {
            var vm = new CreateMaterialViewModel
            {
                Material = new MaterialModel(),
                Categories = _dataContext.Categories
                    .Select(b => new SelectListItem
                    {
                        Value = b.Id.ToString(),
                        Text = b.Name
                    }).ToList(),
                Suppliers = new List<SelectListItem>()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMaterial(CreateMaterialViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            using var transaction = await _dataContext.Database.BeginTransactionAsync();

            try
            {
                vm.Material.CurrentQuantity = vm.InitialQuantity;

                _dataContext.Materials.Add(vm.Material);
                await _dataContext.SaveChangesAsync();

                var inventory = new InventoryTransactionModel
                {
                    MaterialId = vm.Material.Id,
                    DateCreated = DateTime.Now,
                    Quantity = vm.InitialQuantity,
                    UnitPrice = vm.UnitPrice,
                    TotalPrice = vm.InitialQuantity * vm.UnitPrice,
                    Type = TransactionType.IN.ToString(),
                    Note = vm.Note
                };

                _dataContext.InventoryTransactions.Add(inventory);
                await _dataContext.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["success"] = "Tạo nguyên liệu thành công";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Lỗi khi lưu dữ liệu");
                return View(vm);
            }
        }

        // =========================
        // 3. SỬA NGUYÊN LIỆU
        // =========================
        [HttpGet]
        public async Task<IActionResult> EditMaterial(int id)
        {
            var material = await _dataContext.Materials.FindAsync(id);
            if (material == null) return NotFound();

            // Lấy supplier hiện tại
            var supplier = await _dataContext.Suppliers
                .FirstOrDefaultAsync(s => s.SupplierId == material.SupplierId);

            if (supplier == null) return NotFound();

            // Load supplier cùng category
            var suppliers = await _dataContext.Suppliers
                .Where(s => s.CategoryId == supplier.CategoryId)
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.SupplierName
                })
                .ToListAsync();

            var vm = new CreateMaterialViewModel
            {
                Material = material,
                Suppliers = suppliers
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMaterial(CreateMaterialViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var material = await _dataContext.Materials.FindAsync(vm.Material.Id);
            if (material == null) return NotFound();

            material.Name = vm.Material.Name;
            material.Unit = vm.Material.Unit;
            material.SupplierId = vm.Material.SupplierId;
            material.ReorderLevel = vm.Material.ReorderLevel;

            _dataContext.Update(material);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Cập nhật thành công";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // 4. NHẬP KHO
        // =========================
        [HttpGet]
        public async Task<IActionResult> Import(int id)
        {
            var material = await _dataContext.Materials.FindAsync(id);
            if (material == null) return NotFound();

            ViewBag.Material = material;

            var latestPrice = await _dataContext.InventoryTransactions
                .Where(t => t.MaterialId == id && t.Type == TransactionType.IN.ToString())
                .OrderByDescending(t => t.DateCreated)
                .Select(t => t.UnitPrice)
                .FirstOrDefaultAsync();

            return View(new InventoryTransactionModel
            {
                MaterialId = id,
                UnitPrice = latestPrice
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(InventoryTransactionModel model)
        {
            var material = await _dataContext.Materials.FindAsync(model.MaterialId);
            if (material == null) return NotFound();

            if (model.Quantity <= 0)
                ModelState.AddModelError("Quantity", "Số lượng phải > 0");

            if (!ModelState.IsValid)
            {
                ViewBag.Material = material;
                return View(model);
            }

            using var transaction = await _dataContext.Database.BeginTransactionAsync();

            try
            {
                material.CurrentQuantity += model.Quantity;

                model.Type = TransactionType.IN.ToString();
                model.DateCreated = DateTime.Now;
                model.TotalPrice = model.Quantity * model.UnitPrice;

                _dataContext.InventoryTransactions.Add(model);
                _dataContext.Materials.Update(material);

                await _dataContext.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["success"] = "Nhập kho thành công";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await transaction.RollbackAsync();

                var materialReload = await _dataContext.Materials.FindAsync(model.MaterialId);
                ViewBag.Material = materialReload;

                return View(model);
            }
        }

        // =========================
        // 5. XUẤT KHO
        // =========================
        [HttpGet]
        public async Task<IActionResult> Export(int id)
        {
            var material = await _dataContext.Materials.FindAsync(id);
            if (material == null) return NotFound();

            ViewBag.Material = material;

            return View(new InventoryTransactionModel
            {
                MaterialId = id
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Export(InventoryTransactionModel model)
        {
            var material = await _dataContext.Materials.FindAsync(model.MaterialId);
            if (material == null) return NotFound();

            if (model.Quantity > material.CurrentQuantity)
                ModelState.AddModelError("", "Không đủ hàng");

            if (!ModelState.IsValid)
            {
                ViewBag.Material = material;
                return View(model);
            }

            using var transaction = await _dataContext.Database.BeginTransactionAsync();

            try
            {
                material.CurrentQuantity -= model.Quantity;

                model.Type = TransactionType.OUT.ToString();
                model.DateCreated = DateTime.Now;

                _dataContext.InventoryTransactions.Add(model);
                _dataContext.Materials.Update(material);

                await _dataContext.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["success"] = "Xuất kho thành công";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await transaction.RollbackAsync();

                var materialReload = await _dataContext.Materials.FindAsync(model.MaterialId);
                ViewBag.Material = materialReload;

                return View(model);
            }
        }

        // =========================
        // 6. LỊCH SỬ NHẬP KHO
        // =========================
        public async Task<IActionResult> ImportHistory()
        {
            var list = await _dataContext.InventoryTransactions
                .Include(t => t.Material)
                .Where(t => t.Type == TransactionType.IN.ToString())
                .OrderByDescending(t => t.DateCreated)
                .ToListAsync();

            ViewBag.TotalCost = list.Any() ? list.Sum(x => x.TotalPrice) : 0;

            return View(list);
        }
        [HttpGet]
        public IActionResult GetSuppliersByCategory(int categoryId)
        {
            var suppliers = _dataContext.Suppliers
                .Where(s => s.CategoryId == categoryId)
                .Select(s => new
                {
                    id = s.SupplierId,
                    name = s.SupplierName
                })
                .ToList();

            return Json(suppliers);
        }
    }
}