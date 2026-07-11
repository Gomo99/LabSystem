using LaboratoryTestRequestManagementSystem.AppStatus;
using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.Models
{
    public class Consumable
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string ConsumableName { get; set; } = null!;

        public int ReorderLevel { get; set; }
        public int QuantityOnHand { get; set; }

        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;

        public Status Status { get; set; } = Status.Active;
        public ICollection<TestTypeConsumable> TestTypeConsumables { get; set; } = new List<TestTypeConsumable>();
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        // Inside Consumable.cs, add this property
        public int? LowStockThreshold { get; set; } // if null, default to 5
    }
}