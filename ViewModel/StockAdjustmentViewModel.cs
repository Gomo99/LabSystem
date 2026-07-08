namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class StockAdjustmentViewModel
    {
        public int ConsumableId { get; set; }
        public string AdjustmentType { get; set; } = "Increase";
        public int Quantity { get; set; }
    }
}