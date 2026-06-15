using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Models;
using QuanLyNhaHangDemo.Repository;
using QuanLyNhaHangDemo.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyNhaHangDemo.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class KitchenController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly IOrderStateService _orderState;

        public KitchenController(DataContext context, IOrderStateService orderState)
        {
            _dataContext = context;
            _orderState = orderState;
        }

        public async Task<IActionResult> Index(int? selectedKitchenId)
        {
            var filterDate = DateTime.Today; // Thêm ngày hôm nay để hệ thống không định vị nhầm vào các món rác ngày cũ
            var kitchen = await _dataContext.Kitchen.OrderBy(k => k.SortOrder).ToListAsync();

            int defaultKitchenId = kitchen.FirstOrDefault()?.Id ?? 0;

            if (selectedKitchenId.HasValue)
            {
                ViewBag.SelectedKitchenId = selectedKitchenId.Value;
            }
            else
            {
                var activeKitchenIds = await _dataContext.OrderDetails
                    .Where(od => od.IsFired
                                 && od.CreateDate >= filterDate // <-- THÊM DÒNG NÀY để lọc chính xác đơn trong ngày
                                 && od.Status != StatusProduct.Done
                                 && od.Status != StatusProduct.Served
                                 && od.Status != StatusProduct.Cancelled
                                 && od.Product != null
                                 && od.Product.Category != null)
                    .Select(od => od.Product.Category.KitchenId)
                    .Distinct()
                    .ToListAsync();

                if (activeKitchenIds.Any())
                {
                    var firstActiveKitchen = kitchen.FirstOrDefault(k => activeKitchenIds.Contains(k.Id));
                    ViewBag.SelectedKitchenId = firstActiveKitchen?.Id ?? defaultKitchenId;
                }
                else
                {
                    ViewBag.SelectedKitchenId = defaultKitchenId;
                }
            }

            return View(kitchen);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(KitchenModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            _dataContext.Kitchen.Add(model);
            await _dataContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var kitchen = await _dataContext.Kitchen.FindAsync(id);
            return View(kitchen);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(KitchenModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            _dataContext.Kitchen.Update(model);
            await _dataContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var kitchen = await _dataContext.Kitchen.FindAsync(id);
            if (kitchen != null)
            {
                _dataContext.Kitchen.Remove(kitchen);
                await _dataContext.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<IActionResult> KitchenScreen(int kitchenId)
        {
            var filterDate = DateTime.Today;

            var tasks = await _dataContext.OrderDetails
                .Include(od => od.Product)
                    .ThenInclude(p => p.Category)
                .Include(od => od.OrderDetailModifiers)
                    .ThenInclude(odm => odm.Modifier)
                .Where(od => od.Product.Category.KitchenId == kitchenId
                             && od.IsFired
                             && od.CreateDate >= filterDate
                             && od.Status != StatusProduct.Done
                             && od.Status != StatusProduct.Served
                             && od.Status != StatusProduct.Cancelled)
                // 🚀 SỬA ĐOẠN NÀY:
                .OrderByDescending(od => od.FireCount > 1 && (od.Status == StatusProduct.Pending || od.Status == StatusProduct.PreparingIngredient || od.Status == StatusProduct.Cooking))
                .ThenByDescending(od => od.IsManuallyFired)
                .ThenBy(od => od.FiredAt)
                // ────────────────
                .ToListAsync();

            ViewBag.Kitchen = await _dataContext.Kitchen.FindAsync(kitchenId);
            return PartialView("_KitchenBoard", tasks);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderDetailId, int status, string? returnAction)
        {
            var orderDetail = await _dataContext.OrderDetails
                .Include(od => od.Product)
                    .ThenInclude(p => p.Category)
                .Include(od => od.OrderDetailModifiers)
                    .ThenInclude(odm => odm.Modifier)
                        .ThenInclude(m => m.ModifierGroup)
                .Include(od => od.Order)
                .FirstOrDefaultAsync(od => od.Id == orderDetailId);

            if (orderDetail == null)
            {
                return NotFound();
            }

            StatusProduct oldStatus = orderDetail.Status;
            StatusProduct newStatus = (StatusProduct)status;

            // CẬP NHẬT TRẠNG THÁI MỚI VÀ ĐỒNG THỜI GHI NHẬN MỐC THỜI GIAN UPDATE THỰC TẾ
            orderDetail.Status = newStatus;
            orderDetail.UpdatedAt = DateTime.Now; // <-- ĐỒNG BỘ: Điểm mấu chốt để reset thời gian 15p ngầm ở Index đơn hàng!

            bool isJustCompleted = (oldStatus != StatusProduct.Done && newStatus == StatusProduct.Done);

            if (isJustCompleted)
            {
                decimal sizeMultiplier = 1.0m;

                var sizeModifier = orderDetail.OrderDetailModifiers
                    .FirstOrDefault(odm => odm.Modifier != null
                                        && odm.Modifier.ModifierGroup != null
                                        && odm.Modifier.ModifierGroup.Type.Equals("SIZE", StringComparison.OrdinalIgnoreCase));

                if (sizeModifier != null)
                {
                    sizeMultiplier = sizeModifier.Modifier.Multiplier;
                }

                // ============================================================
                // PHẦN 1: TẢI DỮ LIỆU NGUYÊN LIỆU MÓN GỐC VÀ MODIFIER
                // ============================================================
                var productMaterials = await _dataContext.productMaterials
                    .Where(pm => pm.ProductId == orderDetail.ProductId)
                    .Include(pm => pm.Material)
                    .ToListAsync();

                var mainMaterialIds = productMaterials
                    .Where(pm => pm.Material != null)
                    .Select(pm => pm.MaterialId)
                    .Distinct()
                    .ToList();

                var itemModifiers = orderDetail.OrderDetailModifiers
                    .Where(odm => odm.Modifier != null
                               && odm.Modifier.ModifierGroup != null
                               && !odm.Modifier.ModifierGroup.Type.Equals("SIZE", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var modifierIds = itemModifiers.Select(odm => odm.ModifierId).Distinct().ToList();

                var modifierMaterials = await _dataContext.ModifierMaterials
                    .Where(mm => modifierIds.Contains(mm.ModifierId))
                    .Include(mm => mm.Material)
                    .ToListAsync();

                var modMaterialIds = modifierMaterials
                    .Where(mm => mm.Material != null)
                    .Select(mm => mm.MaterialId)
                    .Distinct()
                    .ToList();

                var allMaterialIds = mainMaterialIds.Union(modMaterialIds).Distinct().ToList();

                // ============================================================
                // SỬA LỖI: LẤY GIÁ NHẬP GẦN NHẤT AN TOÀN (Gom dữ liệu về RAM để tránh lỗi dịch LINQ)
                // ============================================================
                var rawImports = await _dataContext.InventoryTransactions
                    .AsNoTracking()
                    .Where(t => allMaterialIds.Contains(t.MaterialId) && t.Type == "IN")
                    .ToListAsync();

                var lastImports = rawImports
                    .GroupBy(t => t.MaterialId)
                    .Select(g => g.OrderByDescending(t => t.DateCreated)
                                  .ThenByDescending(t => t.Id)
                                  .FirstOrDefault())
                    .Where(t => t != null) // Lọc bỏ giá trị null an toàn
                    .ToDictionary(t => t.MaterialId, t => t.UnitPrice);

                // ============================================================
                // PHẦN 3: TIẾN HÀNH TRỪ KHO MÓN GỐC
                // ============================================================
                foreach (var pm in productMaterials)
                {
                    if (pm.Material == null) continue;

                    decimal usedBaseQuantity = (orderDetail.Quantity * pm.QuantityRequired) * sizeMultiplier;
                    pm.Material.CurrentQuantity -= usedBaseQuantity;

                    lastImports.TryGetValue(pm.MaterialId, out decimal unitCost);

                    // Nếu FireCount >= 1 → đây là lần làm lại → đánh dấu OUT_REMAKE
                    string exportReason = orderDetail.FireCount >= 1 ? "OUT_REMAKE" : "OUT_SALE";

                    _dataContext.InventoryTransactions.Add(new InventoryTransactionModel
                    {
                        MaterialId = pm.MaterialId,
                        DateCreated = DateTime.Now,
                        Quantity = usedBaseQuantity,
                        UnitPrice = unitCost,
                        TotalPrice = usedBaseQuantity * unitCost,
                        Type = "OUT",
                        Reason = exportReason,
                        OrderId = orderDetail.OrderId,
                        Note = exportReason == "OUT_REMAKE"
                            ? $"Xuất làm lại (Bếp) — Đơn {orderDetail.Order?.OrderCode} (lần {orderDetail.FireCount})"
                            : $"Xuất bán (Bếp) — Đơn {orderDetail.Order?.OrderCode}"
                    });
                }

                // ============================================================
                // PHẦN 4: TRỪ KHO NGUYÊN LIỆU CÁC MODIFIER ĐI KÈM
                // ============================================================
                if (itemModifiers.Any())
                {
                    foreach (var odm in itemModifiers)
                    {
                        var currentModMaterials = modifierMaterials.Where(mm => mm.ModifierId == odm.ModifierId);
                        foreach (var mm in currentModMaterials)
                        {
                            if (mm.Material == null) continue;

                            decimal usedModifierQuantity = orderDetail.Quantity * mm.QuantityRequired;
                            mm.Material.CurrentQuantity -= usedModifierQuantity;

                            lastImports.TryGetValue(mm.MaterialId, out decimal unitCostMod);

                            _dataContext.InventoryTransactions.Add(new InventoryTransactionModel
                            {
                                MaterialId = mm.MaterialId,
                                DateCreated = DateTime.Now,
                                Quantity = usedModifierQuantity,
                                UnitPrice = unitCostMod,
                                TotalPrice = usedModifierQuantity * unitCostMod,
                                Type = "OUT",
                                Reason = "OUT_SALE",
                                OrderId = orderDetail.OrderId,
                                Note = $"Xuất bán (modifier) — Đơn {orderDetail.Order?.OrderCode}"
                            });
                        }
                    }
                }
            }

            // Đồng bộ cập nhật thay đổi dữ liệu của OrderDetail vào database
            _dataContext.OrderDetails.Update(orderDetail);
            await _dataContext.SaveChangesAsync();

            // Chạy service xử lý trạng thái đồng bộ sau khi đổi trạng thái món ăn
            await _orderState.SyncAfterOrderDetailStatusChangeAsync(orderDetailId, oldStatus, newStatus);

            TempData["success"] = "Cập nhật trạng thái và trừ kho nguyên liệu thành công.";

            int kitchenId = orderDetail.Product?.Category?.KitchenId ?? 0;

            if (!string.IsNullOrEmpty(returnAction))
            {
                return RedirectToAction(returnAction, new { selectedKitchenId = kitchenId });
            }

            return RedirectToAction("Index", new { selectedKitchenId = kitchenId });
        }
    }
}