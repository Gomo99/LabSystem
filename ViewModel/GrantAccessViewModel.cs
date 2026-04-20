namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class GrantAccessViewModel
    {
        public int DoctorId { get; set; }
        public List<int> SelectedTestRequestIds { get; set; } = new();
    }
}
