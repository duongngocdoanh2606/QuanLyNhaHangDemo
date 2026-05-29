using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;

namespace QuanLyNhaHangDemo.Models
{
    public class ModifierGroupModel
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Tên group không được để trống.")]
        public string Name { get; set; }
        public bool IsRequired { get; set; }
        public int MaxSelect { get; set; }
        public int DisplayOrder { get; set; }
        public int Status { get; set; } = 1; // 1: Active, 0: Inactive
        [Required]
        public string Type { get; set; } = "Topping"; // "Topping", "Size", "Extra"
        public virtual ICollection<ModifierModel> Modifiers { get; set; }
        public virtual ICollection<ProModifierGroupModel> ProductModifierMappings { get; set; }

    }
    public class ModifierModel
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Tên modifier không được để trống.")]
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int ModifierGroupId { get; set; }
        public virtual ModifierGroupModel ModifierGroup { get; set; }
        public int Status { get; set; } = 1; // 1: Active, 0: Inactive

        public decimal Multiplier { get; set; } = 1;
        public virtual ICollection<ModifierMaterialModel> ModifierMaterials { get; set; }

    }
    public class  ModifierMaterialModel
    {
        public int Id { get; set; }
        public int ModifierId { get; set; }
        public virtual ModifierModel Modifier { get; set; }
        public int MaterialId { get; set; }
        public virtual MaterialModel Material { get; set; }
        public decimal QuantityRequired { get; set; }
    }
}
