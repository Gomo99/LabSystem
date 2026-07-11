namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class ReceiveSampleViewModel
    {
        public int TestRequestId { get; set; }
        public string PatientName { get; set; } = null!;
        public string DoctorName { get; set; } = null!;
        public DateTime RequestDate { get; set; }
        public List<SampleItemToReceiveViewModel> Samples { get; set; } = new();

        // Helper computed properties
        public int ExpectedSampleCount => Samples.Count;
        public int ReceivedCount => Samples.Count(s => s.IsReceived);
        public int DamagedCount => Samples.Count(s => s.IsDamaged);
        public int MissingCount => ExpectedSampleCount - ReceivedCount - DamagedCount;
    }
}