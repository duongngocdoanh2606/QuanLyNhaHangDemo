using Microsoft.AspNetCore.Mvc.Rendering;

namespace QuanLyNhaHangDemo.Models.ViewModels
{
    public class SupplierViewModel
    {
        public SupplierModel Supplier { get; set; }

        // Dropdown Brand
        public List<SelectListItem> Brands { get; set; }
    }
}
