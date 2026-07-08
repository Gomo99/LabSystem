using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class ConsumableViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Consumable Name")]
        public string ConsumableName { get; set; } = string.Empty;

        [Display(Name = "Reorder Level")]
        public int ReorderLevel { get; set; }

        [Display(Name = "Quantity On Hand")]
        public int QuantityOnHand { get; set; }

        [Display(Name = "Supplier")]
        public int SupplierId { get; set; }
    }
}