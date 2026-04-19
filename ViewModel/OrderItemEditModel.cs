using LaboratoryTestRequestManagementSystem.AppStatus;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class OrderItemEditModel
    {
        public int OrderItemId { get; set; }
        public int ConsumableId { get; set; }
        public string ConsumableName { get; set; } = string.Empty;
        public int QuantityOrdered { get; set; }
        public OrderItemStatus Status { get; set; }
        public bool Remove { get; set; } // Flag for removal in POST
    }
}