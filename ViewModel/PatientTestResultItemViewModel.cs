namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class PatientTestResultItemViewModel
    {
        public string TestName { get; set; } = string.Empty;
        public string? ResultValue { get; set; }
        public string? Units { get; set; }
        public string? NormalRange { get; set; }
        public bool IsAbnormal { get; set; }
        public string? Notes { get; set; }
        public DateTime? CompletedDate { get; set; }
    }
}
