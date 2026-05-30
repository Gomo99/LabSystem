using LaboratoryTestRequestManagementSystem.AppStatus;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class PatientListViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SouthAfricanIdNumber { get; set; } = string.Empty;
        public string CellphoneNumber { get; set; } = string.Empty;
        public Status IsActive { get; set; }

        public int? RegisteredByDoctorId { get; set; }
        public string RegisteredByDoctorName { get; set; } = "Self";
    }
}
