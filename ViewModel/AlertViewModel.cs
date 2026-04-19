namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class AlertViewModel
    {
        public int TestRequestId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string TestName { get; set; } = string.Empty;
        public string ResultValue { get; set; } = string.Empty;
        public string? NormalRange { get; set; }
        public DateTime? CompletedDate { get; set; }
    }
}
