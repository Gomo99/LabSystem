namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class ReceiveSampleViewModel
    {
        public int TestRequestId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public List<SampleItemToReceiveViewModel> Samples { get; set; } = new();
    }
}
