using LaboratoryTestRequestManagementSystem.AppStatus;
using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.Models
{
    public class Supplier
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string SupplierName { get; set; } = null!;

        [StringLength(100)]
        public string? ContactPerson { get; set; }

        [EmailAddress]
        public string? EmailAddress { get; set; }
        public Status Status { get; set; } = Status.Active;
        public ICollection<Consumable> Consumables { get; set; } = new List<Consumable>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}