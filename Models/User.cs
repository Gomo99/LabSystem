namespace LaboratoryTestRequestManagementSystem.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Email { get; set; } = null!; // username

        public string PasswordHash { get; set; } = null!;

        public string Role { get; set; } = null!;

        public bool IsActive { get; set; } = true;

        public int FailedAttempts { get; set; }

        public DateTime? LockoutEnd { get; set; }

        public bool MustChangePassword { get; set; } = true;

        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }
    }
}
