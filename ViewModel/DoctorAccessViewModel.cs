namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class DoctorAccessViewModel
    {
        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime GrantedDate { get; set; }
        public bool HasAccess { get; set; }
        public List<int> SharedTestRequestIds { get; set; } = new();
    }
}