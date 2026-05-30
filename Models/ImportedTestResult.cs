namespace LaboratoryTestRequestManagementSystem.Models
{
    public class ImportedTestResult
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string TestName { get; set; }
        public string ResultValue { get; set; }
        public string Units { get; set; }
        public string NormalRange { get; set; }
        public DateTime? ResultDate { get; set; }
        public string LabName { get; set; }  // original lab

        public Patient Patient { get; set; }
    }
}
