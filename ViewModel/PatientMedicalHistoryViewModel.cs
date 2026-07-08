namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class PatientMedicalHistoryViewModel
    {
        public int PatientId { get; set; }
        public string MedicalConditionsInput { get; set; } = string.Empty;
        public string AllergiesInput { get; set; } = string.Empty;
        public string MedicationsInput { get; set; } = string.Empty;
    }
}