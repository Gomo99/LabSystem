namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class TrackingDataPoint
    {
        public DateTime? Date { get; set; }
        public string? Value { get; set; }
        public bool IsAbnormal { get; set; }
    }
}