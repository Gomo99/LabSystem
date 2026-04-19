using LaboratoryTestRequestManagementSystem.AppStatus;
using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string OrderNumber { get; set; } = null!;

        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;

        public DateTime OrderDate { get; set; } = DateTime.Now;

        // Business status (Order lifecycle)
        public OrderStatus OrderStatus { get; set; } = OrderStatus.Ordered;

        public DateTime? DateCompleted { get; set; }
        public DateTime? DateCancelled { get; set; }
        public string? CancellationReason { get; set; }

        // Soft delete / system status (ACTIVE / INACTIVE)
        public Status Status { get; set; } = Status.Active;

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}