using Microsoft.AspNetCore.Mvc.Rendering;

namespace QuanLyNhaHangDemo.Models.ViewModels
{
    public class MaterialViewModel
    {
        public MaterialModel Material { get; set; }
        public List<SelectListItem> Suppliers {  get; set; }
    }
}
