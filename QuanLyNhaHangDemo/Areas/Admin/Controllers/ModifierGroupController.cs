using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Models;
using QuanLyNhaHangDemo.Repository;

namespace QuanLyNhaHangDemo.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class ModifierGroupController : Controller
    {
        private readonly DataContext _dataContext;

        public ModifierGroupController(DataContext context)
        {
            _dataContext = context;
        }

        public async Task<IActionResult> Index()
        {
            // Lấy danh sách nhóm kèm theo số lượng tùy chọn bên trong
            return View(await _dataContext.ModifierGroups.Include(g => g.Modifiers).ToListAsync());
        }

        [HttpGet]
        public IActionResult Create()
        {
            // Gửi danh sách nguyên liệu để Admin chọn liên kết cho từng Modifier
            ViewBag.Materials = _dataContext.Materials.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ModifierGroupModel group)
        {
            if (ModelState.IsValid)
            {
                if (group.Modifiers != null)
                {
                    foreach (var mod in group.Modifiers)
                    {
                        if (group.Type == "SIZE")
                        {
                            // Size thì không dùng bảng trung gian ModifierMaterials nữa
                            mod.ModifierMaterials?.Clear();

                            // Nếu admin quên không nhập hệ số nhân, mặc định đặt là 1 (giữ nguyên định mức gốc)
                            if (mod.Multiplier <= 0) mod.Multiplier = 1;
                        }
                        else if (mod.ModifierMaterials != null)
                        {
                            mod.ModifierMaterials = mod.ModifierMaterials.Where(mm => mm.MaterialId > 0).ToList();
                        }
                    }
                }

                _dataContext.Add(group);
                await _dataContext.SaveChangesAsync();
                TempData["success"] = "Tạo nhóm tùy chọn thành công";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Materials = _dataContext.Materials.ToList();
            return View(group);
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            // Nạp đầy đủ thông tin từ nhóm đến Modifiers và cả ModifierMaterials của từng Modifier
            var group = await _dataContext.ModifierGroups
                .Include(g => g.Modifiers)
                    .ThenInclude(m => m.ModifierMaterials)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null) return NotFound();

            ViewBag.Materials = await _dataContext.Materials.ToListAsync();
            return View(group);
        }

        // POST: Admin/ModifierGroup/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ModifierGroupModel group)
        {
            if (id != group.Id) return NotFound();

            // Xóa validation của Modifier con để tránh lỗi đồng bộ mảng trống ngoài ý muốn
            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Modifiers")).ToList())
            {
                ModelState.Remove(key);
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Materials = await _dataContext.Materials.ToListAsync();
                return View(group);
            }

            try
            {
                var dbGroup = await _dataContext.ModifierGroups
                    .Include(g => g.Modifiers)
                        .ThenInclude(m => m.ModifierMaterials)
                    .FirstOrDefaultAsync(g => g.Id == id);

                if (dbGroup == null) return NotFound();

                // 1. Cập nhật thông tin nhóm chính
                dbGroup.Name = group.Name;
                dbGroup.Type = group.Type;
                dbGroup.MaxSelect = group.Type == "SIZE" ? 1 : group.MaxSelect;
                dbGroup.IsRequired = group.Type == "SIZE" ? true : group.IsRequired;

                var incomingModifiers = group.Modifiers?.ToList() ?? new List<ModifierModel>();

                // 2. Xử lý Xóa các Modifier không còn xuất hiện trên giao diện
                var incomingIds = incomingModifiers.Where(x => x.Id > 0).Select(x => x.Id).ToList();
                var deleteMods = dbGroup.Modifiers.Where(x => !incomingIds.Contains(x.Id)).ToList();
                foreach (var item in deleteMods)
                {
                    if (item.ModifierMaterials != null && item.ModifierMaterials.Any())
                    {
                        _dataContext.ModifierMaterials.RemoveRange(item.ModifierMaterials);
                    }
                    _dataContext.Modifiers.Remove(item);
                }

                // 3. Xử lý Thêm mới / Cập nhật các Modifier
                foreach (var incoming in incomingModifiers)
                {
                    if (incoming.Id == 0) // THÊM MỚI
                    {
                        var newMod = new ModifierModel
                        {
                            Name = incoming.Name,
                            Price = incoming.Price,
                            Multiplier = group.Type == "SIZE" ? (incoming.Multiplier <= 0 ? 1 : incoming.Multiplier) : 1,
                            ModifierGroupId = dbGroup.Id,
                            ModifierMaterials = new List<ModifierMaterialModel>()
                        };

                        if (group.Type != "SIZE" && incoming.ModifierMaterials != null)
                        {
                            var mat = incoming.ModifierMaterials.FirstOrDefault();
                            if (mat != null && mat.MaterialId > 0)
                            {
                                newMod.ModifierMaterials.Add(new ModifierMaterialModel
                                {
                                    MaterialId = mat.MaterialId,
                                    QuantityRequired = mat.QuantityRequired > 0 ? mat.QuantityRequired : 1
                                });
                            }
                        }
                        dbGroup.Modifiers.Add(newMod);
                    }
                    else // CẬP NHẬT DÒNG CŨ
                    {
                        var dbMod = dbGroup.Modifiers.FirstOrDefault(x => x.Id == incoming.Id);
                        if (dbMod == null) continue;

                        dbMod.Name = incoming.Name;
                        dbMod.Price = incoming.Price;
                        dbMod.Multiplier = group.Type == "SIZE" ? (incoming.Multiplier <= 0 ? 1 : incoming.Multiplier) : 1;

                        if (dbMod.ModifierMaterials == null) dbMod.ModifierMaterials = new List<ModifierMaterialModel>();

                        if (group.Type == "SIZE")
                        {
                            // Nếu đổi thành nhóm SIZE -> Xóa định mức nguyên liệu tĩnh gắn riêng của nó
                            if (dbMod.ModifierMaterials.Any())
                            {
                                _dataContext.ModifierMaterials.RemoveRange(dbMod.ModifierMaterials);
                            }
                        }
                        else // Nhóm TOPPING
                        {
                            var incomingMat = incoming.ModifierMaterials?.FirstOrDefault();
                            var dbMat = dbMod.ModifierMaterials.FirstOrDefault();

                            if (incomingMat != null && incomingMat.MaterialId > 0)
                            {
                                if (dbMat == null)
                                {
                                    dbMod.ModifierMaterials.Add(new ModifierMaterialModel
                                    {
                                        MaterialId = incomingMat.MaterialId,
                                        QuantityRequired = incomingMat.QuantityRequired > 0 ? incomingMat.QuantityRequired : 1
                                    });
                                }
                                else
                                {
                                    dbMat.MaterialId = incomingMat.MaterialId;
                                    dbMat.QuantityRequired = incomingMat.QuantityRequired > 0 ? incomingMat.QuantityRequired : 1;
                                }
                            }
                            else if (dbMat != null)
                            {
                                _dataContext.ModifierMaterials.Remove(dbMat);
                            }
                        }
                    }
                }

                await _dataContext.SaveChangesAsync();
                TempData["success"] = "Cập nhật nhóm tùy chọn thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                ViewBag.Materials = await _dataContext.Materials.ToListAsync();
                return View(group);
            }
        }


        // GET: Admin/ModifierGroup/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            // 1. Tải nhóm tùy chọn lên kèm theo danh sách Modifiers (Option con)
            var group = await _dataContext.ModifierGroups
                .Include(g => g.Modifiers)
                    .ThenInclude(m => m.ModifierMaterials)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group != null)
            {
                try
                {
                    // 2. XÓA BƯỚC 1: Xóa toàn bộ liên kết giữa Nhóm này và các Món ăn (Giải quyết lỗi FK bạn đang gặp)
                    var productMappings = _dataContext.Set<ProModifierGroupModel>() // Thay đúng tên Model bảng ProductModifierMappings của bạn
                        .Where(pm => pm.ModifierGroupId == id);
                    _dataContext.RemoveRange(productMappings);

                    // 3. XÓA BƯỚC 2: Duyệt qua từng Option con để xóa sạch định lượng nguyên liệu kho (nếu có)
                    foreach (var mod in group.Modifiers)
                    {
                        if (mod.ModifierMaterials != null && mod.ModifierMaterials.Any())
                        {
                            _dataContext.ModifierMaterials.RemoveRange(mod.ModifierMaterials);
                        }
                    }

                    // 4. XÓA BƯỚC 3: Xóa toàn bộ Option con (ví dụ: Size M, Size L, Trân châu...)
                    _dataContext.Modifiers.RemoveRange(group.Modifiers);

                    // 5. XÓA BƯỚC 4: Cuối cùng, xóa Nhóm tùy chọn chính
                    _dataContext.ModifierGroups.Remove(group);

                    // Lưu toàn bộ quá trình xóa xuống Database theo đúng thứ tự gọn gàng
                    await _dataContext.SaveChangesAsync();

                    TempData["success"] = "Đã xóa nhóm tùy chọn và các liên kết liên quan thành công";
                }
                catch (Exception ex)
                {
                    TempData["error"] = "Không thể xóa nhóm này do lỗi: " + ex.Message;
                }
            }

            return RedirectToAction(nameof(Index));
        }

    }
}
