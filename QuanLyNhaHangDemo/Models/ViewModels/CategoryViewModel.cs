using Microsoft.AspNetCore.Mvc.Rendering;

namespace QuanLyNhaHangDemo.Models.ViewModels
{
    public class CategoryViewModel
    {
        public CategoryModel Category { get; set; }

        public List<SelectListItem> Kitchen { get; set; }
    }
}
