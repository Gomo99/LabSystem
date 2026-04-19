using LaboratoryTestRequestManagementSystem.AppStatus;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class EditOrderViewModel
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public OrderStatus OrderStatus { get; set; }

        // List of items currently on the order
        public List<OrderItemEditModel> Items { get; set; } = new List<OrderItemEditModel>();

        // For adding new items
        public int? NewConsumableId { get; set; }
        public int? NewQuantity { get; set; }
    }
}
