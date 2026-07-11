namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class PatientHistoryItem
    {
        public int TestRequestId { get; set; }
        public DateTime RequestDate { get; set; }
        public string DoctorName { get; set; } = null!;
        public string TestName { get; set; } = null!;
        public string? ResultValue { get; set; }
        public bool IsAbnormal { get; set; }
        public DateTime? ResultDate { get; set; }
    }
}
