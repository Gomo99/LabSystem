using LaboratoryTestRequestManagementSystem.Models;

namespace LaboratoryTestRequestManagementSystem.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlBody);
        Task SendAsync(string toEmail, string subject, string htmlBody); // optional, keep for backward compatibility
        Task SendEmailWithAttachmentAsync(string toEmail, string subject, string body, byte[] attachmentBytes, string attachmentFileName);
    }


    public interface INotificationService
    {
        Task CreateAsync(int userId, string userType, string message, string link = null);
        Task<int> GetUnreadCountAsync(int userId, string userType);
        Task<List<Notification>> GetNotificationsAsync(int userId, string userType);
        Task<List<Notification>> GetRecentNotificationsAsync(int userId, string userType, int count); // NEW
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(int userId, string userType);
        Task DeleteAsync(int notificationId);
        Task ClearAllAsync(int userId, string userType);
    }


    public interface IPdfReportService
    {
        Task<byte[]> GenerateTestPerformanceReport(DateTime startDate, DateTime endDate);
        Task<byte[]> GenerateTestResultsPdf(int testRequestId);
        Task<byte[]> GenerateDoctorTestRequestsReport(int doctorId, DateTime startDate, DateTime endDate);
        Task<byte[]> GenerateTechnicianCompletedTestsReport(int technicianId, DateTime startDate, DateTime endDate);

        // ✅ New method for patient's results report grouped by category
        Task<byte[]> GeneratePatientResultsReport(int patientId, DateTime startDate, DateTime endDate);
    }



    public interface ITwoFactorService
    {
        string GenerateSecretKey();
        string GetQrCodeUri(string secretKey, string email, string issuer);
        byte[] GenerateQrCodePng(string uri);
        bool VerifyCode(string secretKey, string code);
        List<string> GenerateRecoveryCodes();
        bool VerifyRecoveryCode(string storedJson, string inputCode, out string updatedJson);
    }




}
