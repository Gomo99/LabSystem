using Microsoft.AspNetCore.Mvc.Rendering;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class PatientTrackingViewModel
    {
        public int? SelectedTestTypeId { get; set; }
        public SelectList TestTypeOptions { get; set; } = null!;
        public string? TestName { get; set; }
        public string? Units { get; set; }
        public decimal? NormalMin { get; set; }
        public decimal? NormalMax { get; set; }
        public List<TrackingDataPoint> DataPoints { get; set; } = new();
    }
}