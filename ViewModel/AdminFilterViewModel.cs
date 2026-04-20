namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class AdminFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public string? Category { get; set; }
        public bool ShowInactive { get; set; } = false;
    }
}