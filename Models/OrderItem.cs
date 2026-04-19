using LaboratoryTestRequestManagementSystem.AppStatus;

namespace LaboratoryTestRequestManagementSystem.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public int ConsumableId { get; set; }
        public Consumable Consumable { get; set; } = null!;

        public int QuantityOrdered { get; set; }

        // Business workflow status
        public OrderItemStatus OrderItemStatus { get; set; } = OrderItemStatus.Ordered;

        public Status Status { get; set; } = Status.Active;

        public DateTime? DateReceived { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime? DateCancelled { get; set; }
    }
}