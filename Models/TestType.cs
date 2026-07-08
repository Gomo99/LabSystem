using LaboratoryTestRequestManagementSystem.AppStatus;
using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.Models
{
    public class TestType
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string TestName { get; set; } = null!;

        public int TestCategoryId { get; set; }
        public TestCategory TestCategory { get; set; } = null!;

        public int SampleTypeId { get; set; }
        public SampleType SampleType { get; set; } = null!;

        [StringLength(50)]
        public string? UnitsOfMeasurement { get; set; }

        public decimal? NormalRangeMin { get; set; }
        public decimal? NormalRangeMax { get; set; }
        public Status Status { get; set; } = Status.Active;
        public int TurnaroundTimeMinutes { get; set; }

        // Many-to-many with Consumable
        public ICollection<TestTypeConsumable> TestTypeConsumables { get; set; } = new List<TestTypeConsumable>();

        // Many-to-many with LabTechnician (for assignment)
        public ICollection<TechnicianTestType> TechnicianTestTypes { get; set; } = new List<TechnicianTestType>();
    }
}