using LaboratoryTestRequestManagementSystem.AppStatus;
using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.Models
{
    public class TestCategory
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string CategoryName { get; set; } = null!;

        public string? Description { get; set; }

        public ICollection<TestType> TestTypes { get; set; } = new List<TestType>();

        public Status Status { get; set; } = Status.Active;
    }
}