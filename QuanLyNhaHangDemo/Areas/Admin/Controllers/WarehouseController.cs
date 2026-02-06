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

        // 1. Danh sách nguyên liệu
        public async Task<IActionResult> Index()
        {
            var materials = await _dataContext.Materials
                .Include(m=>m.Supplier)
                .OrderBy(m => m.Name)
                .ToListAsync();

            var transactions = await _dataContext.InventoryTransactions
                .Where(t => t.Type == "IN")
                .ToListAsync();

            // Tìm ngày mới nhất cho mỗi nguyên liệu và lấy đơn giá nhập kho tương ứng
            var latestDates = transactions
                .GroupBy(t => t.MaterialId)
                .Select(g => new
                {
                    MaterialId = g.Key,
                    LatestDate = g.Max(x => x.DateCreated)
                })
                .ToList();

            // Lấy đơn giá nhập kho mới nhất cho mỗi nguyên liệu
            var latestTxs = (from t in transactions
                             join l in latestDates on t.MaterialId equals l.MaterialId
                             where t.DateCreated == l.LatestDate
                             select new
                             {
                                 t.MaterialId,
                                 t.UnitPrice
                             })
                             .ToList();

            // Chuyển đổi danh sách giao dịch mới nhất thành từ điển
            var latestPriceByMaterial = latestTxs
                .GroupBy(x => x.MaterialId)
                .ToDictionary(g => g.Key, g => g.First().UnitPrice);

            // Truyền giá trị vào ViewBag để hiển thị trong View
            ViewBag.LatestPriceByMaterial = latestPriceByMaterial;
            

            return View(materials);
        }


        // 2. Tạo nguyên liệu
        [HttpGet]
        public IActionResult CreateMaterial()
        {
            var vm = new CreateMaterialViewModel
            {
                Material = new MaterialModel(),
                Brands = _dataContext.Brands
                    .Select(b => new SelectListItem
                    {
                        Value = b.Id.ToString(),
                        Text = b.Name
                    }).ToList(),
                Suppliers = new List<SelectListItem>() // ban đầu rỗng
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
                    Type = "IN", 
                    Note = vm.Note
                };

                _dataContext.InventoryTransactions.Add(inventory);
                await _dataContext.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["success"] = "Tạo nguyên liệu và nhập kho ban đầu thành công";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Có lỗi khi lưu dữ liệu");
                return View(vm);
            }
        }


        // 3. Sửa nguyên liệu
        [HttpGet]
        public async Task<IActionResult> EditMaterial(int id)
        {
            var material = await _dataContext.Materials.FindAsync(id);
            if (material == null) return NotFound();

            return View(material);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMaterial(MaterialModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var material = await _dataContext.Materials.FindAsync(model.Id);
            if (material == null) return NotFound();

            material.Name = model.Name;
            material.Unit = model.Unit;
            material.ReorderLevel = model.ReorderLevel;
            material.Status = model.Status;

            _dataContext.Materials.Update(material);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Cập nhật nguyên liệu thành công";
            return RedirectToAction(nameof(Index));
        }

        // 4. Nhập kho
        [HttpGet]
        public async Task<IActionResult> Import(int id)
        {
            var material = await _dataContext.Materials.FindAsync(id);
            if (material == null) return NotFound();

            ViewBag.Material = material;

            // lấy đơn giá nhập mới nhất (Type == "IN") cho material này
            var latestUnitPrice = await _dataContext.InventoryTransactions
                .Where(t => t.MaterialId == id && t.Type == "IN")
                .OrderByDescending(t => t.DateCreated)
                .ThenByDescending(t => t.Id)
                .Select(t => t.UnitPrice)
                .FirstOrDefaultAsync();

            var model = new InventoryTransactionModel
            {
                MaterialId = id,
                DateCreated = DateTime.Now,
                Type = "IN",
                UnitPrice = latestUnitPrice // nếu không có giao dịch trước đó -> 0
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(InventoryTransactionModel model)
        {
            var material = await _dataContext.Materials.FindAsync(model.MaterialId);
            if (material == null) return NotFound();

            if (model.Quantity <= 0)
                ModelState.AddModelError("Quantity", "Số lượng phải > 0");

            if (model.UnitPrice < 0)
                ModelState.AddModelError("UnitPrice", "Đơn giá không hợp lệ");

            if (!ModelState.IsValid)
            {
                ViewBag.Material = material;
                model.Id = 0;
                return View(model);
            }
            model.Id = 0;
            model.Type = "IN";
            model.DateCreated = DateTime.Now;
            model.TotalPrice = model.Quantity * model.UnitPrice;

            material.CurrentQuantity += model.Quantity;

            _dataContext.InventoryTransactions.Add(model);
            _dataContext.Materials.Update(material);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Nhập kho thành công";
            return RedirectToAction(nameof(Index));
        }

        // New: Lịch sử nhập kho (chỉ IN transactions)
        [HttpGet]
        public async Task<IActionResult> ImportHistory(DateTime? from, DateTime? to)
        {
            var query = _dataContext.InventoryTransactions
                .Include(t => t.Material)  // Bao gồm thông tin nguyên liệu
                .Where(t => t.Type == "IN")  // Chỉ lấy các giao dịch nhập kho
                .AsQueryable();

            // Lọc theo khoảng thời gian nếu có
            if (from.HasValue)
                query = query.Where(t => t.DateCreated >= from.Value.Date);
            if (to.HasValue)
                query = query.Where(t => t.DateCreated <= to.Value.Date);

            var importHistory = await query
                .OrderByDescending(t => t.DateCreated)  // Sắp xếp theo ngày nhập
                .ThenByDescending(t => t.Id)
                .ToListAsync();

            // Tính tổng chi phí nhập kho
            var totalImportCost = importHistory.Sum(t => t.TotalPrice);

            // Trả về dữ liệu lịch sử nhập kho và tổng chi phí
            ViewBag.TotalImportCost = totalImportCost;  // Lưu tổng chi phí nhập kho vào ViewBag
            return View(importHistory);
        }

        // Lịch sử nhập kho (chỉ IN transactions)
        [HttpGet]
        public async Task<IActionResult> ImportHistoryTungMon(int id, DateTime? from, DateTime? to)
        {
            var material = await _dataContext.Materials.FindAsync(id);
            if (material == null) return NotFound();

            var query = _dataContext.InventoryTransactions
                .Where(t => t.Type == "IN" && t.MaterialId == id)
                .AsQueryable();

            if (from.HasValue)
                query = query.Where(t => t.DateCreated >= from.Value.Date);
            if (to.HasValue)
                query = query.Where(t => t.DateCreated <= to.Value.Date);

            var list = await query
                .OrderByDescending(t => t.DateCreated)
                .ThenByDescending(t => t.Id)
                .ToListAsync();

            ViewBag.Material = material;
            ViewBag.TotalImportQuantity = list.Sum(x => x.Quantity);
            ViewBag.TotalImportCost = list.Sum(x => x.TotalPrice);

            return View(list);
        }




        // 6. Báo cáo doanh thu + chi phí nguyên liệu (dựa trên bảng Statisticals + InventoryTransactions)
        [HttpGet]
        public async Task<IActionResult> RevenueReport(DateTime? from, DateTime? to)
        {
            var stats = _dataContext.Statisticals.AsQueryable();
            var imports = _dataContext.InventoryTransactions
                .Where(t => t.Type == "IN")
                .AsQueryable();

            if (from.HasValue)
            {
                stats = stats.Where(s => s.DateCreated >= from.Value);
                imports = imports.Where(t => t.DateCreated >= from.Value);
            }

            if (to.HasValue)
            {
                stats = stats.Where(s => s.DateCreated <= to.Value);
                imports = imports.Where(t => t.DateCreated <= to.Value);
            }

            var totalRevenue = await stats.SumAsync(s => s.Revenue);
            var totalProfit = await stats.SumAsync(s => s.Profit);
            var totalImportCost = await imports.SumAsync(t => t.TotalPrice);

            ViewBag.From = from;
            ViewBag.To = to;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalProfit = totalProfit;
            ViewBag.TotalImportCost = totalImportCost;
            ViewBag.NetProfit = totalRevenue - totalImportCost;

            return View();
        }
        [HttpGet]
        public IActionResult GetSuppliersByBrand(int brandId)
        {
            var suppliers = _dataContext.Suppliers
                .Where(s => s.BrandId == brandId)
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
