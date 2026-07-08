namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class OrderCreateViewModel
    {
        public int SupplierId { get; set; }
        public Dictionary<int, int> ItemQuantities { get; set; } = new();
    }
}