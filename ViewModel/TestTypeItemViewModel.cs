using LaboratoryTestRequestManagementSystem.AppStatus;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class TestTypeItemViewModel
    {
        public string TestName { get; set; } = string.Empty;
        public string SampleType { get; set; } = string.Empty;
        public RequestStatus Status { get; set; }

        public string? ResultValue { get; set; }
        public bool IsAbnormal { get; set; }
        public string? Notes { get; set; }

        public string? NormalRange { get; set; }
    }
}
