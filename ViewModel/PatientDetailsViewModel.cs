namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class PatientDetailsViewModel
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string SouthAfricanIdNumber { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string CellphoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string HomeAddress { get; set; } = string.Empty;

        // Medical history
        public List<string> MedicalConditions { get; set; } = new();
        public List<string> Allergies { get; set; } = new();
        public List<string> Medications { get; set; } = new();
    }
}