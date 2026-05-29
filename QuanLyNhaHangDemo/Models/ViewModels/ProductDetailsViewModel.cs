using System.ComponentModel.DataAnnotations;

namespace QuanLyNhaHangDemo.Models.ViewModels
{
    public class ProductDetailsViewModel
    {
        public ProductModel ProductDetails { get; set; }
        public string Comment { get; set; }
        [Required(ErrorMessage = "Yeu cau nhap ten thuong hieu")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Yeu cau nhap ten thuong hieu")]
        public string Email { get; set; }

    }
}
