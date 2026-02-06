using System.ComponentModel.DataAnnotations;

namespace QuanLyNhaHangDemo.Models
{
    public class KitchenModel
    {
        public int Id { get; set; }
        [Required]
        [Display(Name="Tên khu bếp")]
        public string Name { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
        public ICollection<CategoryModel> Categories { get; set; }  = new List<CategoryModel>();

    }
}
