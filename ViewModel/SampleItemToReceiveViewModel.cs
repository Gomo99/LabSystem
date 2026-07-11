namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class SampleItemToReceiveViewModel
    {
        public int SampleId { get; set; }
        public string Barcode { get; set; } = null!;
        public string SampleType { get; set; } = null!;
        public bool IsReceived { get; set; }
        public DateTime? ReceivedDate { get; set; }

        public bool IsDamaged { get; set; }
        public string? RejectionReason { get; set; }
    }
}