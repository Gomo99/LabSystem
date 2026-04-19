namespace LaboratoryTestRequestManagementSystem.Services
{
    public interface IPdfReportService
    {
        Task<byte[]> GenerateTestPerformanceReport(DateTime startDate, DateTime endDate);
        Task<byte[]> GenerateTestResultsPdf(int testRequestId);

        // ✅ New method for doctor's test requests report
        Task<byte[]> GenerateDoctorTestRequestsReport(int doctorId, DateTime startDate, DateTime endDate);
    }
}