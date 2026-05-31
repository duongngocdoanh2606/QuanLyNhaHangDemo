using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QuanLyNhaHangDemo.Models.ViewModels
{
        public class CreateMaterialViewModel
        {
            public MaterialModel Material { get; set; }

            [Display(Name = "Danh mục nhà cung cấp")]
            public int SupplierCategoryId { get; set; }

            [Required]
            [Display(Name = "Số lượng nhập ban đầu")]
            public decimal InitialQuantity { get; set; }

            [Required]
            [Display(Name = "Đơn giá")]
            public decimal UnitPrice { get; set; }

            [Display(Name = "Ghi chú")]
            public string? Note { get; set; }
            public List<SelectListItem> SupplierCategories { get; set; }
            public List<SelectListItem> Suppliers { get; set; }
        }

    
}
