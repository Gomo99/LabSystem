using LaboratoryTestRequestManagementSystem.AppStatus;
using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.Models
{
    public class Employee
    {
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; } = null!;

        [Required]
        public string LastName { get; set; } = null!;

        // Login username (email)
        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        // Legacy field – can be kept but not used for login
        public string? Username { get; set; }

        [Required]
        public string PasswordHash { get; set; } = null!;

        [Phone]
        public string? ContactNumber { get; set; }

        // Doctor specific
        [StringLength(20)]
        public string? HPCSANumber { get; set; }      // Unique for doctors

        // Technician specific
        [StringLength(13)]
        public string? SAIDNumber { get; set; }       // Unique for technicians
        public string? EmployeeNumber { get; set; }

        public UserRole Role { get; set; }
        public Status IsActive { get; set; } = Status.Active;
        public int FailedAttempts { get; set; }
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }
        public bool MustChangePassword { get; set; } = false;
        public string? EmailVerificationTokenHash { get; set; }
        public DateTime? EmailVerificationTokenExpires { get; set; }
        public bool IsTwoFactorEnabled { get; set; } = false;
        public string? TwoFactorSecretKey { get; set; }
        public string? TwoFactorRecoveryCodes { get; set; }
        public string? ResetPin { get; set; }
        public DateTime? ResetPinExpiration { get; set; }
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutEnd { get; set; }

        // Navigation for technician test type assignments
        public ICollection<TechnicianTestType> TechnicianTestTypes { get; set; } = new List<TechnicianTestType>();
    }
}