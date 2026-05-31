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

        // Lý do xuất kho
        public static class ExportReason
        {
            public const string Sale = "OUT_SALE";
            public const string Return = "OUT_RETURN";
            public const string Manual = "OUT_MANUAL";

            public static string GetLabel(string? reason) => reason switch
            {
                Sale => "Xuất bán hàng",
                Return => "Trả hàng lỗi NCC",
                Manual => "Xuất thủ công",
                _ => reason ?? "Không rõ"
            };
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

        // Hàm trợ giúp nạp Dropdown tùy biến theo ID danh mục được chọn độc lập
        private async Task PopulateDropdownsNewAsync(CreateMaterialViewModel vm, int? selectedCategoryId)
        {
            // Tải danh mục nhà cung cấp
            vm.SupplierCategories = await _dataContext.SupplierCategories
                .Where(c => c.IsActive)
                .Select(b => new SelectListItem
                {
                    Value = b.SupplierCategoryId.ToString(),
                    Text = b.Name
                }).ToListAsync();

            // Nếu form lỗi, dựa vào ID danh mục NCC truyền từ Form về để tải lại danh sách Nhà cung cấp tương ứng
            if (selectedCategoryId.HasValue && selectedCategoryId.Value > 0)
            {
                vm.Suppliers = await _dataContext.Suppliers
                    .Where(s => s.SupplierCategoryId == selectedCategoryId.Value)
                    .Select(s => new SelectListItem
                    {
                        Value = s.SupplierId.ToString(),
                        Text = s.SupplierName
                    }).ToListAsync();

                ViewBag.SelectedCategoryId = selectedCategoryId; // Đẩy ngược lại View để giữ selected trên UI
            }
            else
            {
                vm.Suppliers = new List<SelectListItem>();
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreateMaterial()
        {
            var vm = new CreateMaterialViewModel
            {
                Material = new MaterialModel()
            };
            await PopulateDropdownsNewAsync(vm, null);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMaterial(CreateMaterialViewModel vm, int? SelectedCategoryId)
        {
            // Xóa validation của các đối tượng liên kết hệ thống để tránh lỗi ngầm
            ModelState.Remove("Material.Supplier");

            // Validate ExpiryDate nếu có: không cho ngày hết hạn nhỏ hơn ngày hôm nay (tuỳ policy)
            if (vm.Material?.ExpiryDate.HasValue == true)
            {
                var expiry = vm.Material.ExpiryDate.Value.Date;
                if (expiry < DateTime.Today)
                    ModelState.AddModelError("Material.ExpiryDate", "Hạn sử dụng không được nhỏ hơn hôm nay.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsNewAsync(vm, SelectedCategoryId);
                return View(vm);
            }

            // 1. Lấy ra chiến lược thực thi được cấu hình trong hệ thống
            var executionStrategy = _dataContext.Database.CreateExecutionStrategy();

            try
            {
                // 2. Chạy toàn bộ Transaction bên trong chiến lược thử lại (Retriable Unit)
                await executionStrategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _dataContext.Database.BeginTransactionAsync();

                    try
                    {
                        vm.Material.CurrentQuantity = vm.InitialQuantity;
                        vm.Material.Status = 1;

                        // 🔥 THÊM DÒNG NÀY ĐỂ FIX LỖI IDENTITY:
                        vm.Material.Id = 0;

                        // Bước 1: Thêm mới Nguyên liệu
                        _dataContext.Materials.Add(vm.Material);
                        await _dataContext.SaveChangesAsync();

                        // Bước 2: Tạo hóa đơn/Lịch sử nhập kho ban đầu
                        var inventory = new InventoryTransactionModel
                        {
                            MaterialId = vm.Material.Id,
                            DateCreated = DateTime.Now,
                            Quantity = vm.InitialQuantity,
                            UnitPrice = vm.UnitPrice,
                            TotalPrice = vm.InitialQuantity * vm.UnitPrice,
                            Type = TransactionType.IN.ToString(),
                            Note = string.IsNullOrEmpty(vm.Note) ? "Nhập kho khởi tạo" : vm.Note
                        };

                        _dataContext.InventoryTransactions.Add(inventory);
                        await _dataContext.SaveChangesAsync();

                        // Xác nhận giao dịch thành công toàn vẹn dữ liệu
                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        // Nếu có bất kỳ lỗi nào bên trong, thực hiện Rollback lập tức
                        await transaction.RollbackAsync();
                        throw; // Bắt buộc phải throw để ExecutionStrategy biết có lỗi và thực hiện thử lại nếu cần
                    }
                });

                TempData["success"] = "Tạo nguyên liệu thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Lấy thông báo lỗi sâu nhất (InnerException)
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

                // In lỗi ra tab Output/Console của Visual Studio để bạn dễ đọc
                System.Diagnostics.Debug.WriteLine("=== LỖI TẬN GỐC EF CORE: " + innerMessage);

                // Hiển thị trực tiếp lỗi cụ thể lên giao diện View công khai
                ModelState.AddModelError("", "Lỗi cụ thể từ Database: " + innerMessage);

                await PopulateDropdownsNewAsync(vm, SelectedCategoryId);
                return View(vm);
            }
        }

        // =========================
        // 3. SỬA NGUYÊN LIỆU
        // =========================
        [HttpGet]
        public async Task<IActionResult> EditMaterial(int id)
        {
            // 1. Tìm nguyên liệu cần chỉnh sửa
            var material = await _dataContext.Materials.FindAsync(id);
            if (material == null) return NotFound();

            // 2. Lấy danh sách toàn bộ nhà cung cấp (Bỏ bớt bộ lọc ngặt nghèo để tránh lỗi Null)
            var suppliers = await _dataContext.Suppliers
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(), // Hãy chắc chắn thuộc tính là SupplierId hoặc Id
                    Text = s.SupplierName           // Hãy chắc chắn thuộc tính là SupplierName hoặc Name
                })
                .ToListAsync();

            // 3. Đóng gói vào ViewModel
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
            // Validate ExpiryDate: không cho đặt hạn sử dụng nhỏ hơn hôm nay
            if (vm.Material?.ExpiryDate.HasValue == true)
            {
                var expiry = vm.Material.ExpiryDate.Value.Date;
                if (expiry < DateTime.Today)
                    ModelState.AddModelError("Material.ExpiryDate", "Hạn sử dụng không được nhỏ hơn hôm nay.");
            }

            if (!ModelState.IsValid)
            {
                // 🔥 QUAN TRỌNG: Nếu form lỗi, bắt buộc phải nạp lại danh sách dữ liệu Suppliers cho Dropdown, 
                // nếu không giao diện Razor View sẽ bị crash (báo lỗi NullReferenceException)
                vm.Suppliers = await _dataContext.Suppliers
                    .Select(s => new SelectListItem
                    {
                        Value = s.SupplierId.ToString(),
                        Text = s.SupplierName
                    })
                    .ToListAsync();

                return View(vm);
            }

            // Tìm thực thể gốc từ cơ sở dữ liệu để cập nhật tracking
            var material = await _dataContext.Materials.FindAsync(vm.Material.Id);
            if (material == null) return NotFound();

            // Cập nhật các thông tin thay đổi từ màn hình chỉnh sửa
            material.Name = vm.Material.Name;
            material.Unit = vm.Material.Unit;
            material.SupplierId = vm.Material.SupplierId;
            material.ReorderLevel = vm.Material.ReorderLevel;
            material.ExpiryDate = vm.Material.ExpiryDate; // <-- cập nhật Hạn sử dụng

            // Lưu thay đổi vào hệ thống cơ sở dữ liệu
            _dataContext.Update(material);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Cập nhật nguyên liệu thành công!";
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

            ModelState.Remove("Material");
            ModelState.Remove("Type");
            ModelState.Remove("Reason");
            ModelState.Remove("DateCreated");
            ModelState.Remove("TotalPrice");

            if (!ModelState.IsValid)
            {
                ViewBag.Material = material;
                return View(model);
            }

            // ====================================================================
            // 🔥 SỬA TẠI ĐÂY: Khởi tạo chiến lược thực thi để hỗ trợ Retry Transaction
            // ====================================================================
            var strategy = _dataContext.Database.CreateExecutionStrategy();

            try
            {
                // Chạy toàn bộ tiến trình bên trong ExecuteAsync
                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _dataContext.Database.BeginTransactionAsync();
                    try
                    {
                        // 1. Cập nhật số lượng vật liệu
                        material.CurrentQuantity += model.Quantity;
                        _dataContext.Materials.Update(material);

                        // 2. Điền đầy đủ các trường bắt buộc của bảng Transaction
                        model.Type = TransactionType.IN.ToString();
                        model.Reason = "IN_IMPORT";
                        model.DateCreated = DateTime.Now;
                        model.TotalPrice = model.Quantity * model.UnitPrice;

                        // 🔥 SỬA CHÍ MẠNG TẠI ĐÂY: Ép Id về 0 để SQL Server tự động tăng ID, tránh lỗi IDENTITY_INSERT
                        model.Id = 0;

                        _dataContext.InventoryTransactions.Add(model);

                        // 3. Thực thi lưu xuống DB và commit
                        await _dataContext.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });

                TempData["success"] = "Nhập kho thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Đọc lại vật liệu để đổ lại dữ liệu cho View nếu thất bại
                ViewBag.Material = await _dataContext.Materials.FindAsync(model.MaterialId);

                var dbError = ex.Message;
                if (ex.InnerException != null)
                {
                    dbError = ex.InnerException.Message;
                    if (ex.InnerException.InnerException != null)
                    {
                        dbError = ex.InnerException.InnerException.Message;
                    }
                }

                ModelState.AddModelError("", "Lỗi thực tế từ SQL: " + dbError);
                TempData["error"] = "Có lỗi xảy ra khi nhập kho!";

                return View(model);
            }
        }

        // =========================
        // 5. XUẤT KHO (THỦ CÔNG)
        // =========================
        [HttpGet]
        public async Task<IActionResult> Export(int id)
        {
            var material = await _dataContext.Materials
                .Include(m => m.Supplier)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (material == null) return NotFound();

            ViewBag.Material = material;

            // Danh sách NCC để chọn khi trả hàng lỗi
            ViewBag.Suppliers = await _dataContext.Suppliers
                .Where(s => s.Status == SupplierStatus.Active)
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.SupplierName
                })
                .ToListAsync();

            // Giá nhập gần nhất làm gợi ý đơn giá
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
        public async Task<IActionResult> Export(InventoryTransactionModel model)
        {
            var material = await _dataContext.Materials.FindAsync(model.MaterialId);
            if (material == null) return NotFound();

            if (model.Quantity <= 0)
                ModelState.AddModelError("Quantity", "Số lượng phải > 0");

            if (model.Quantity > material.CurrentQuantity)
                ModelState.AddModelError("Quantity", $"Không đủ hàng trong kho (tồn hiện tại: {material.CurrentQuantity})");

            // Xóa validation của các trường hệ thống tự sinh để vượt qua IsValid
            ModelState.Remove("Material");
            ModelState.Remove("Type");
            ModelState.Remove("DateCreated");
            ModelState.Remove("TotalPrice");

            if (!ModelState.IsValid)
            {
                ViewBag.Material = material;
                ViewBag.Suppliers = await _dataContext.Suppliers
                    .Where(s => s.Status == SupplierStatus.Active)
                    .Select(s => new SelectListItem { Value = s.SupplierId.ToString(), Text = s.SupplierName })
                    .ToListAsync();
                return View(model);
            }

            // 🔥 SỬA LỖI 1: Khởi tạo chiến lược thực thi hỗ trợ Retry kết nối
            var strategy = _dataContext.Database.CreateExecutionStrategy();

            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _dataContext.Database.BeginTransactionAsync();
                    try
                    {
                        // 1. Trừ số lượng vật liệu trong kho thô
                        material.CurrentQuantity -= model.Quantity;
                        _dataContext.Materials.Update(material);

                        // 2. Điền đầy đủ các thông tin giao dịch xuất
                        model.Type = TransactionType.OUT.ToString(); // Hoặc "OUT"
                        model.DateCreated = DateTime.Now;
                        model.TotalPrice = model.Quantity * model.UnitPrice;

                        // 🔥 SỬA LỖI 3: Kiểm tra đồng bộ chuỗi lý do trực tiếp từ Form gửi lên
                        if (string.IsNullOrEmpty(model.Reason))
                        {
                            model.Reason = "OUT_MANUAL";
                        }

                        // Nếu không phải xuất trả hàng lỗi (OUT_RETURN) thì ép SupplierId về null
                        if (model.Reason != "OUT_RETURN")
                        {
                            model.SupplierId = null;
                        }

                        // 🔥 SỬA LỖI 2: Ép Id về 0 để SQL Server tự động sinh khóa chính mới
                        model.Id = 0;

                        _dataContext.InventoryTransactions.Add(model);

                        // 3. Thực thi lưu xuống DB và commit
                        await _dataContext.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });

                TempData["success"] = "Xuất kho thủ công thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Đọc lại dữ liệu để đổ lại vào View khi xảy ra lỗi cay đắng
                ViewBag.Material = await _dataContext.Materials.FindAsync(model.MaterialId);
                ViewBag.Suppliers = await _dataContext.Suppliers
                    .Where(s => s.Status == SupplierStatus.Active)
                    .Select(s => new SelectListItem { Value = s.SupplierId.ToString(), Text = s.SupplierName })
                    .ToListAsync();

                // Bóc tách lỗi chi tiết từ SQL hiển thị lên giao diện debug
                var dbError = ex.Message;
                if (ex.InnerException != null)
                {
                    dbError = ex.InnerException.Message;
                    if (ex.InnerException.InnerException != null)
                    {
                        dbError = ex.InnerException.InnerException.Message;
                    }
                }

                ModelState.AddModelError("", "Lỗi thực tế từ SQL: " + dbError);
                TempData["error"] = "Có lỗi xảy ra khi xuất kho!";

                return View(model);
            }
        }

        // =========================
        // 6. LỊCH SỬ NHẬP KHO
        // =========================
        public async Task<IActionResult> ImportHistory(int? materialId, DateTime? from, DateTime? to)
        {
            var query = _dataContext.InventoryTransactions
                .Include(t => t.Material)
                    .ThenInclude(m => m.Supplier)
                .Where(t => t.Type == TransactionType.IN.ToString())
                .AsQueryable();

            if (materialId.HasValue)
                query = query.Where(t => t.MaterialId == materialId.Value);

            if (from.HasValue)
                query = query.Where(t => t.DateCreated.Date >= from.Value.Date);

            if (to.HasValue)
                query = query.Where(t => t.DateCreated.Date <= to.Value.Date);

            var list = await query
                .OrderByDescending(t => t.DateCreated)
                .ToListAsync();

            ViewBag.TotalCost = list.Any() ? list.Sum(x => x.TotalPrice) : 0;
            ViewBag.Materials = await _dataContext.Materials
                .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.Name })
                .ToListAsync();
            ViewBag.SelectedMaterial = materialId;
            ViewBag.From = from;
            ViewBag.To = to;

            return View(list);
        }

        // =========================
        // 7. LỊCH SỬ XUẤT KHO
        // =========================
        public async Task<IActionResult> ExportHistory(string? reason, int? materialId, DateTime? from, DateTime? to)
        {
            var query = _dataContext.InventoryTransactions
                .Include(t => t.Material)
                    .ThenInclude(m => m.Supplier)
                .Include(t => t.Supplier)
                .Include(t => t.Order)
                .Where(t => t.Type == TransactionType.OUT.ToString())
                .AsQueryable();

            if (!string.IsNullOrEmpty(reason))
                query = query.Where(t => t.Reason == reason);

            if (materialId.HasValue)
                query = query.Where(t => t.MaterialId == materialId.Value);

            if (from.HasValue)
                query = query.Where(t => t.DateCreated.Date >= from.Value.Date);

            if (to.HasValue)
                query = query.Where(t => t.DateCreated.Date <= to.Value.Date);

            var list = await query
                .OrderByDescending(t => t.DateCreated)
                .ToListAsync();

            ViewBag.TotalCost = list.Sum(x => x.TotalPrice);
            ViewBag.TotalSale = list.Where(x => x.Reason == ExportReason.Sale).Sum(x => x.TotalPrice);
            ViewBag.TotalReturn = list.Where(x => x.Reason == ExportReason.Return).Sum(x => x.TotalPrice);
            ViewBag.TotalManual = list.Where(x => x.Reason == ExportReason.Manual).Sum(x => x.TotalPrice);
            ViewBag.SelectedReason = reason;
            ViewBag.SelectedMaterial = materialId;
            ViewBag.From = from;
            ViewBag.To = to;
            ViewBag.Materials = await _dataContext.Materials
                .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.Name })
                .ToListAsync();
            ViewBag.ExportReason = typeof(ExportReason);

            return View(list);
        }

        // =========================
        // 8. IN HÓA ĐƠN NHẬP KHO
        // =========================
        [HttpGet]
        public async Task<IActionResult> PrintImportInvoice(int id)
        {
            var tx = await _dataContext.InventoryTransactions
                .Include(t => t.Material)
                    .ThenInclude(m => m.Supplier)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tx == null || tx.Type != TransactionType.IN.ToString())
                return NotFound();

            return View(tx);
        }

        // =========================
        // 9. IN HÓA ĐƠN XUẤT KHO
        // =========================
        [HttpGet]
        public async Task<IActionResult> PrintExportInvoice(int id)
        {
            var tx = await _dataContext.InventoryTransactions
                .Include(t => t.Material)
                    .ThenInclude(m => m.Supplier)
                .Include(t => t.Supplier)
                .Include(t => t.Order)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tx == null || tx.Type != TransactionType.OUT.ToString())
                return NotFound();

            // Nếu OUT_SALE → lấy tất cả nguyên liệu tiêu hao của đơn đó
            List<InventoryTransactionModel>? orderOutList = null;
            if (tx.Reason == ExportReason.Sale && tx.OrderId.HasValue)
            {
                orderOutList = await _dataContext.InventoryTransactions
                    .Include(t => t.Material)
                    .Where(t => t.OrderId == tx.OrderId && t.Type == TransactionType.OUT.ToString() && t.Reason == ExportReason.Sale)
                    .ToListAsync();
            }

            ViewBag.OrderOutList = orderOutList;
            ViewBag.ExportReason = ExportReason.GetLabel(tx.Reason);
            return View(tx);
        }

        // =========================
        // 10. SUPPLIERS BY SUPPLIER CATEGORY (AJAX)
        // =========================
        [HttpGet]
        public IActionResult GetSuppliersByCategory(int categoryId)
        {
            var suppliers = _dataContext.Suppliers
                .Where(s => s.SupplierCategoryId == categoryId)
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