// Models/ConsumableUsageHistory.cs
using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.Models
{
    public class ConsumableUsageHistory
    {
        public int Id { get; set; }

        public int ConsumableId { get; set; }
        public Consumable Consumable { get; set; } = null!;

        public int QuantityUsed { get; set; }

        public DateTime UsageDate { get; set; } = DateTime.Now;

        // Which test triggered this usage (optional but useful)
        public int? TestRequestId { get; set; }
        public TestRequest? TestRequest { get; set; }

        public int? TestTypeId { get; set; }
        public TestType? TestType { get; set; }

        public int? TechnicianId { get; set; }
        public Employee? Technician { get; set; }
    }
}