namespace LaboratoryTestRequestManagementSystem.Services
{
    public interface IPdfReportService
    {
        Task<byte[]> GenerateTestPerformanceReport(DateTime startDate, DateTime endDate);
        Task<byte[]> GenerateTestResultsPdf(int testRequestId);
        Task<byte[]> GenerateDoctorTestRequestsReport(int doctorId, DateTime startDate, DateTime endDate);
        Task<byte[]> GenerateTechnicianCompletedTestsReport(int technicianId, DateTime startDate, DateTime endDate);
        Task<byte[]> GeneratePatientResultsReport(int patientId, DateTime startDate, DateTime endDate);
        // IPdfReportService.cs
        Task<byte[]> GenerateCancellationPdf(int testRequestId);
    }
}