using LaboratoryTestRequestManagementSystem.AppStatus;
using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.Models
{
    public class Allergy
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = null!;

        [StringLength(50)]
        public string? Category { get; set; }

        public Status Status { get; set; } = Status.Active;
        public ICollection<PatientAllergy> PatientAllergies { get; set; } = new List<PatientAllergy>();
    }
}