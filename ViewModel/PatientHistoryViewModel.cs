namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class PatientHistoryViewModel
    {
        public string PatientName { get; set; } = null!;
        public string? BloodGroup { get; set; }
        public string? EmergencyContact { get; set; }
        public List<PatientHistoryItem> History { get; set; } = new();
    }
}
