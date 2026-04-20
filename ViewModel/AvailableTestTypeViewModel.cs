using LaboratoryTestRequestManagementSystem.AppStatus;
using LaboratoryTestRequestManagementSystem.ViewModel;

public class AvailableTestTypeViewModel
{
    public int TestRequestId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
    public Urgency Urgency { get; set; }
    public string? ClinicalNotes { get; set; }  // ✅ Doctor's clinical notes

    // ✅ Patient medical history
    public List<string> MedicalConditions { get; set; } = new();
    public List<string> Allergies { get; set; } = new();
    public List<string> Medications { get; set; } = new();

    public List<TestTypeItemForProcessingViewModel> TestTypes { get; set; } = new();
}