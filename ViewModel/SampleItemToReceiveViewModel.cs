namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class SampleItemToReceiveViewModel
    {
        public int SampleId { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string SampleType { get; set; } = string.Empty;
        public bool IsReceived { get; set; }
        public DateTime? ReceivedDate { get; set; }
    }
}
