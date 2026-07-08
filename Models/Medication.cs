using LaboratoryTestRequestManagementSystem.AppStatus;
using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.Models
{
    public class Medication
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = null!;

        [StringLength(50)]
        public string? Category { get; set; }
        public Status Status { get; set; } = Status.Active;

        public ICollection<PatientMedication> PatientMedications { get; set; } = new List<PatientMedication>();
    }
}