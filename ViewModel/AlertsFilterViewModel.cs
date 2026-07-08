namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class AlertsFilterViewModel
    {
        public DateTime StartDate { get; set; } = DateTime.Today.AddDays(-5);
        public DateTime EndDate { get; set; } = DateTime.Today;
        public List<AlertViewModel> Alerts { get; set; } = new();
    }
}