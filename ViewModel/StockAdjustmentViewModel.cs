using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class StockAdjustmentViewModel
    {
        [Required(ErrorMessage = "Consumable is required.")]
        public int ConsumableId { get; set; }

        [Required(ErrorMessage = "Adjustment type is required.")]
        [RegularExpression("^(Increase|Decrease|Set)$", ErrorMessage = "Invalid adjustment type.")]
        public string AdjustmentType { get; set; } = "Increase";

        [Required(ErrorMessage = "Quantity is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be a positive number greater than zero.")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "A reason for this adjustment is required.")]
        [StringLength(250, ErrorMessage = "Reason cannot exceed 250 characters.")]
        public string Reason { get; set; }
    }
}