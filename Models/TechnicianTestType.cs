using LaboratoryTestRequestManagementSystem.AppStatus;

namespace LaboratoryTestRequestManagementSystem.Models
{
    public class TechnicianTestType
    {
        public int TechnicianId { get; set; }
        public Employee Technician { get; set; } = null!; // Role = LabTechnician

        public int TestTypeId { get; set; }
        public TestType TestType { get; set; } = null!;

        public Status Status { get; set; } = Status.Active;
    }
}