using LaboratoryTestRequestManagementSystem.AppStatus;

namespace LaboratoryTestRequestManagementSystem.Models
{
    public class PatientAllergy
    {
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        public int AllergyId { get; set; }
        public Allergy Allergy { get; set; } = null!;

        public Status Status { get; set; } = Status.Active;

    }
}