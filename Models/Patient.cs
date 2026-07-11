using LaboratoryTestRequestManagementSystem.AppStatus;
using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.Models
{
    public class Patient
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string FirstName { get; set; } = null!;

        [Required, StringLength(50)]
        public string LastName { get; set; } = null!;

        [Required, StringLength(13)]
        public string SouthAfricanIdNumber { get; set; } = null!;

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required, StringLength(20)]
        public string CellphoneNumber { get; set; } = null!;

        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public string HomeAddress { get; set; } = null!;

        [Required]
        public string PasswordHash { get; set; } = null!;

        public Status IsActive { get; set; } = Status.Active;

        public bool MustChangePassword { get; set; } = false;
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }
        public Status Status { get; set; } = Status.Active;

        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutEnd { get; set; }

        // NEW – who registered the patient (null = self‑registered)
        public int? RegisteredByDoctorId { get; set; }
        public Employee? RegisteredByDoctor { get; set; }

        // Navigation properties
        public ICollection<PatientCondition> PatientConditions { get; set; } = new List<PatientCondition>();
        public ICollection<PatientAllergy> PatientAllergies { get; set; } = new List<PatientAllergy>();
        public ICollection<PatientMedication> PatientMedications { get; set; } = new List<PatientMedication>();
        public ICollection<DoctorPatientAccess> DoctorAccessGrants { get; set; } = new List<DoctorPatientAccess>();


        // Models/Patient.cs
        [StringLength(5)]
        public string? BloodGroup { get; set; }          // e.g., A+, O-, etc.

        [StringLength(100)]
        public string? EmergencyContactName { get; set; }

        [StringLength(20)]
        public string? EmergencyContactNumber { get; set; }

        public ICollection<TestRequest> TestRequests { get; set; } = new List<TestRequest>();

    }
}