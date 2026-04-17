using Microsoft.AspNetCore.Mvc.Rendering;

namespace QuanLyNhaHangDemo.Models.ViewModels
{
    public class SupplierViewModel
    {
        public SupplierModel Supplier { get; set; }
        public List<SelectListItem> Categories { get; set; }
    }
}
