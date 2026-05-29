using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using QuanLyNhaHangDemo.Repository.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace QuanLyNhaHangDemo.Models
{
    public class ProductModel
    {
        [Key]
        public int Id { get; set; }
        [Required, MinLength(4, ErrorMessage = "Yeu cau nhap ten san pham")]
        public string Name { get; set; }
        [Required, MinLength(4, ErrorMessage = "Yeu cau nhap mo ta san pham")]
        public string Description { get; set; }
        public string Slug { get; set; }
        [Required(ErrorMessage = "Yeu cau nhap gia san pham")]
        [Range(1, double.MaxValue, ErrorMessage = "Gia phai lon hon 0")]
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
        public CategoryModel Category { get; set; }
        public int Status { get; set; } = 1;
        public virtual ICollection<ProModifierGroupModel> ProductModifierGroups { get; set; }
        public virtual ICollection<ProductMaterialsModel> ProductMaterials { get; set; }
        public int Sold { get; set; }
        public string Image {  get; set; }
        [NotMapped]
        [FileExtension]
        public IFormFile? ImageUpLoad { get; set; }
    }
}
