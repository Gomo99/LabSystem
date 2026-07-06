using LaboratoryTestRequestManagementSystem.AppStatus;
using LaboratoryTestRequestManagementSystem.Data;
using LaboratoryTestRequestManagementSystem.Hubs;
using LaboratoryTestRequestManagementSystem.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OtpNet;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Net;
using System.Net.Mail;
using System.Text.Json;

namespace LaboratoryTestRequestManagementSystem.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        // Existing methods (unchanged)
        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            await SendAsync(toEmail, subject, htmlBody);
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            await SendWithAttachmentAsync(toEmail, subject, htmlBody, null, null);
        }

        // New method with attachment
        public async Task SendEmailWithAttachmentAsync(string toEmail, string subject, string body, byte[] attachmentBytes, string attachmentFileName)
        {
            await SendWithAttachmentAsync(toEmail, subject, body, attachmentBytes, attachmentFileName);
        }

        private async Task SendWithAttachmentAsync(string toEmail, string subject, string body, byte[]? attachmentBytes, string? attachmentFileName)
        {
            try
            {
                var smtp = _config["Email:Host"];
                var portStr = _config["Email:Port"];
                var user = _config["Email:Username"];
                var pass = _config["Email:Password"];
                var from = _config["Email:SenderEmail"];

                if (string.IsNullOrEmpty(smtp)) throw new Exception("Email Host is not configured.");
                if (string.IsNullOrEmpty(portStr) || !int.TryParse(portStr, out int port)) throw new Exception("Email Port is not configured correctly.");
                if (string.IsNullOrEmpty(user)) throw new Exception("Email Username is missing.");
                if (string.IsNullOrEmpty(pass)) throw new Exception("Email Password is missing.");
                if (string.IsNullOrEmpty(from)) throw new Exception("Email Sender address is missing.");

                using var client = new SmtpClient(smtp, port)
                {
                    Credentials = new NetworkCredential(user, pass),
                    EnableSsl = true
                };

                var message = new MailMessage(from, toEmail, subject, body)
                {
                    IsBodyHtml = true
                };

                // Attach file if provided
                if (attachmentBytes != null && attachmentBytes.Length > 0 && !string.IsNullOrWhiteSpace(attachmentFileName))
                {
                    var stream = new MemoryStream(attachmentBytes);
                    var attachment = new Attachment(stream, attachmentFileName, "application/pdf");
                    message.Attachments.Add(attachment);
                }

                await client.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("EMAIL ERROR: " + ex.Message);
                throw;
            }
        }
    }



    public class NotificationService : INotificationService
    {
        private readonly LabDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(LabDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task CreateAsync(int userId, string userType, string message, string link = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                UserType = userType,
                Message = message,
                Link = link,
                IsRead = false,
                CreatedDate = DateTime.Now,
                Status = Status.Active
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Broadcast the new notification to the target user's group
            string groupName = $"{userType}-{userId}";
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveNotification", new
            {
                id = notification.Id,
                message = notification.Message,
                link = notification.Link,
                createdDate = notification.CreatedDate.ToString("g")
            });

            // Also push the updated unread count
            int unreadCount = await GetUnreadCountAsync(userId, userType);
            await _hubContext.Clients.Group(groupName).SendAsync("UpdateUnreadCount", unreadCount);
        }

        public async Task<int> GetUnreadCountAsync(int userId, string userType)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && n.UserType == userType && !n.IsRead
                            && n.Status == Status.Active)
                .CountAsync();
        }

        public async Task<List<Notification>> GetNotificationsAsync(int userId, string userType)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && n.UserType == userType && n.Status == Status.Active)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();
        }

        // NEW METHOD: Get recent notifications for dropdown
        public async Task<List<Notification>> GetRecentNotificationsAsync(int userId, string userType, int count)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && n.UserType == userType && n.Status == Status.Active)
                .OrderByDescending(n => n.CreatedDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(int userId, string userType)
        {
            var unread = await _context.Notifications
                .Where(n => n.UserId == userId && n.UserType == userType && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread)
                n.IsRead = true;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.Status = Status.Inactive;
                await _context.SaveChangesAsync();
            }
        }

        public async Task ClearAllAsync(int userId, string userType)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && n.UserType == userType && n.Status == Status.Active)
                .ToListAsync();

            foreach (var n in notifications)
                n.Status = Status.Inactive;

            await _context.SaveChangesAsync();
        }



    }


    public class PdfReportService : IPdfReportService
    {
        private readonly LabDbContext _context;

        public PdfReportService(LabDbContext context)
        {
            _context = context;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        #region Test Performance Report (Lab Manager)

        public async Task<byte[]> GenerateTestPerformanceReport(DateTime startDate, DateTime endDate)
        {
            var tests = await _context.TestResults
                .Where(r => r.CompletedDate.HasValue
                            && r.CompletedDate.Value.Date >= startDate.Date
                            && r.CompletedDate.Value.Date <= endDate.Date)
                .Include(r => r.TestType).ThenInclude(t => t.TestCategory)
                .ToListAsync();

            var grouped = tests
                .GroupBy(r => r.TestType.TestCategory.CategoryName)
                .OrderBy(g => g.Key)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToList();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Column(header =>
                    {
                        header.Item().Text("Test Performance Report")
                              .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium)
                              .AlignCenter();

                        header.Item().Text($"Period: {startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}")
                              .FontSize(12).AlignCenter();

                        header.Item().PaddingBottom(20); // spacing after header
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.ConstantColumn(100);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Test Category");
                            header.Cell().Element(CellStyle).Text("Total Tests");

                            static IContainer CellStyle(IContainer container) =>
                                container.DefaultTextStyle(x => x.SemiBold())
                                         .PaddingVertical(5)
                                         .BorderBottom(1)
                                         .BorderColor(Colors.Black);
                        });

                        foreach (var item in grouped)
                        {
                            table.Cell().Text(item.Category);
                            table.Cell().Text(item.Count.ToString());
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        #endregion

        #region Single Test Request Results PDF

        public async Task<byte[]> GenerateTestResultsPdf(int testRequestId)
        {
            var request = await _context.TestRequests
                .Include(r => r.Patient)
                .Include(r => r.Doctor)
                .Include(r => r.TestRequestTestTypes).ThenInclude(trt => trt.TestType)
                .FirstOrDefaultAsync(r => r.Id == testRequestId);

            if (request == null) return Array.Empty<byte>();

            var results = await _context.TestResults
                .Where(r => r.TestRequestId == testRequestId)
                .ToListAsync();

            var samples = await _context.Samples
                .Where(s => s.TestRequestId == testRequestId)
                .Include(s => s.SampleType)
                .ToListAsync();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Text("Laboratory Test Results Report")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Darken2)
                        .AlignCenter();

                    page.Content().Column(column =>
                    {
                        column.Item().Text($"Request #: {request.Id}").SemiBold();
                        column.Item().Text($"Patient: {request.Patient.FirstName} {request.Patient.LastName}");
                        column.Item().Text($"Doctor: Dr. {request.Doctor.LastName}");
                        column.Item().Text($"Request Date: {request.RequestDate:dd/MM/yyyy}");
                        column.Item().Text($"Urgency: {request.Urgency}");

                        column.Item().PaddingTop(20).Text("Test Results").FontSize(16).SemiBold();

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(4);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Test");
                                header.Cell().Element(CellStyle).Text("Result");
                                header.Cell().Element(CellStyle).Text("Normal Range");
                                header.Cell().Element(CellStyle).Text("Notes");

                                static IContainer CellStyle(IContainer container) =>
                                    container.DefaultTextStyle(x => x.SemiBold())
                                             .PaddingVertical(5)
                                             .BorderBottom(1)
                                             .BorderColor(Colors.Black);
                            });

                            foreach (var trt in request.TestRequestTestTypes)
                            {
                                var result = results.FirstOrDefault(r => r.TestTypeId == trt.TestTypeId);
                                table.Cell().Text(trt.TestType.TestName);
                                table.Cell().Text(result?.ResultValue ?? "—");
                                table.Cell().Text(trt.TestType.NormalRangeMin.HasValue && trt.TestType.NormalRangeMax.HasValue
                                    ? $"{trt.TestType.NormalRangeMin} - {trt.TestType.NormalRangeMax} {trt.TestType.UnitsOfMeasurement}"
                                    : "—");
                                table.Cell().Text(result?.Notes ?? "—");
                            }
                        });

                        column.Item().PaddingTop(20).Text("Samples").FontSize(16).SemiBold();

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Barcode");
                                header.Cell().Element(CellStyle).Text("Sample Type");
                                header.Cell().Element(CellStyle).Text("Collected Date");

                                static IContainer CellStyle(IContainer container) =>
                                    container.DefaultTextStyle(x => x.SemiBold())
                                             .PaddingVertical(5)
                                             .BorderBottom(1)
                                             .BorderColor(Colors.Black);
                            });

                            foreach (var sample in samples)
                            {
                                table.Cell().Text(sample.Barcode);
                                table.Cell().Text(sample.SampleType.Name);
                                table.Cell().Text(sample.CollectedDate?.ToString("dd/MM/yyyy HH:mm") ?? "—");
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        #endregion

        #region Doctor's Test Requests Report

        public async Task<byte[]> GenerateDoctorTestRequestsReport(int doctorId, DateTime startDate, DateTime endDate)
        {
            var requests = await _context.TestRequests
                .Where(tr => tr.DoctorId == doctorId
                             && tr.RequestDate.Date >= startDate.Date
                             && tr.RequestDate.Date <= endDate.Date)
                .Include(tr => tr.Patient)
                .Include(tr => tr.TestRequestTestTypes).ThenInclude(trt => trt.TestType)
                .OrderByDescending(tr => tr.RequestDate)
                .ToListAsync();

            var doctor = await _context.Employees.FindAsync(doctorId);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(header =>
                    {
                        header.Item().Text($"Test Requests Report - Dr. {doctor?.LastName}")
                              .SemiBold().FontSize(18).FontColor(Colors.Blue.Darken2)
                              .AlignCenter();

                        header.Item().Text($"Period: {startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}")
                              .FontSize(12).AlignCenter();

                        header.Item().PaddingBottom(20);
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(60);
                            columns.RelativeColumn(2);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(70);
                            columns.RelativeColumn(3);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Req #");
                            header.Cell().Element(CellStyle).Text("Patient");
                            header.Cell().Element(CellStyle).Text("Date");
                            header.Cell().Element(CellStyle).Text("Urgency");
                            header.Cell().Element(CellStyle).Text("Status");
                            header.Cell().Element(CellStyle).Text("Tests");

                            static IContainer CellStyle(IContainer container) =>
                                container.DefaultTextStyle(x => x.SemiBold())
                                         .PaddingVertical(5)
                                         .BorderBottom(1)
                                         .BorderColor(Colors.Black);
                        });

                        foreach (var req in requests)
                        {
                            var testNames = string.Join(", ", req.TestRequestTestTypes.Select(trt => trt.TestType.TestName));
                            table.Cell().Text(req.Id.ToString());
                            table.Cell().Text($"{req.Patient.FirstName} {req.Patient.LastName}");
                            table.Cell().Text(req.RequestDate.ToString("dd/MM/yyyy"));
                            table.Cell().Text(req.Urgency.ToString());
                            table.Cell().Text(req.RequestStatus.ToString());
                            table.Cell().Text(testNames);
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        #endregion

        #region Technician's Completed Tests Report

        public async Task<byte[]> GenerateTechnicianCompletedTestsReport(int technicianId, DateTime startDate, DateTime endDate)
        {
            var completedTests = await _context.TestRequestTestTypes
                .Where(trt => trt.TechnicianId == technicianId
                              && trt.RequestStatus == RequestStatus.Completed
                              && trt.CompletionDateTime.HasValue
                              && trt.CompletionDateTime.Value.Date >= startDate.Date
                              && trt.CompletionDateTime.Value.Date <= endDate.Date)
                .Include(trt => trt.TestType).ThenInclude(tt => tt.TestCategory)
                .Include(trt => trt.TestRequest).ThenInclude(tr => tr.Patient)
                .OrderBy(trt => trt.TestType.TestCategory.CategoryName)
                .ThenBy(trt => trt.CompletionDateTime)
                .ToListAsync();

            var technician = await _context.Employees.FindAsync(technicianId);
            var grouped = completedTests
                .GroupBy(t => t.TestType.TestCategory.CategoryName)
                .OrderBy(g => g.Key)
                .ToList();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(header =>
                    {
                        header.Item().Text($"Completed Tests Report - {technician?.FirstName} {technician?.LastName}")
                              .SemiBold().FontSize(18).FontColor(Colors.Blue.Darken2)
                              .AlignCenter();

                        header.Item().Text($"Period: {startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}")
                              .FontSize(12).AlignCenter();

                        header.Item().PaddingBottom(20);
                    });

                    page.Content().Column(column =>
                    {
                        foreach (var group in grouped)
                        {
                            column.Item().PaddingTop(10).Text(group.Key).FontSize(14).SemiBold();
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(60);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.ConstantColumn(100);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Req #");
                                    header.Cell().Element(CellStyle).Text("Patient");
                                    header.Cell().Element(CellStyle).Text("Test");
                                    header.Cell().Element(CellStyle).Text("Completed");

                                    static IContainer CellStyle(IContainer container) =>
                                        container.DefaultTextStyle(x => x.SemiBold())
                                                 .PaddingVertical(5)
                                                 .BorderBottom(1)
                                                 .BorderColor(Colors.Black);
                                });

                                foreach (var test in group)
                                {
                                    table.Cell().Text(test.TestRequestId.ToString());
                                    table.Cell().Text($"{test.TestRequest.Patient.FirstName} {test.TestRequest.Patient.LastName}");
                                    table.Cell().Text(test.TestType.TestName);
                                    table.Cell().Text(test.CompletionDateTime?.ToString("dd/MM/yyyy HH:mm") ?? "—");
                                }
                            });
                        }

                        column.Item().PaddingTop(20).Text($"Total Tests: {completedTests.Count}").SemiBold();
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        #endregion

        #region Patient's Results Report (Grouped by Category)

        public async Task<byte[]> GeneratePatientResultsReport(int patientId, DateTime startDate, DateTime endDate)
        {
            var results = await _context.TestResults
                .Where(r => r.TestRequest.PatientId == patientId
                            && r.TestRequest.RequestStatus == RequestStatus.ReleasedByDoctor
                            && r.CompletedDate.HasValue
                            && r.CompletedDate.Value.Date >= startDate.Date
                            && r.CompletedDate.Value.Date <= endDate.Date)
                .Include(r => r.TestType).ThenInclude(tt => tt.TestCategory)
                .Include(r => r.TestRequest)
                .OrderBy(r => r.TestType.TestCategory.CategoryName)
                .ThenBy(r => r.CompletedDate)
                .ToListAsync();

            var patient = await _context.Patients.FindAsync(patientId);
            var grouped = results
                .GroupBy(r => r.TestType.TestCategory.CategoryName)
                .OrderBy(g => g.Key)
                .ToList();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(header =>
                    {
                        header.Item().Text($"Test Results Report - {patient?.FirstName} {patient?.LastName}")
                              .SemiBold().FontSize(18).FontColor(Colors.Blue.Darken2)
                              .AlignCenter();

                        header.Item().Text($"Period: {startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}")
                              .FontSize(12).AlignCenter();

                        header.Item().PaddingBottom(20);
                    });

                    page.Content().Column(column =>
                    {
                        if (!results.Any())
                        {
                            column.Item().Text("No results found for the selected period.").FontSize(12).Italic();
                        }
                        else
                        {
                            foreach (var group in grouped)
                            {
                                column.Item().PaddingTop(10).Text(group.Key).FontSize(14).SemiBold();
                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(60);
                                        columns.RelativeColumn(2);
                                        columns.ConstantColumn(80);
                                        columns.ConstantColumn(100);
                                        columns.RelativeColumn(3);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyle).Text("Req #");
                                        header.Cell().Element(CellStyle).Text("Test");
                                        header.Cell().Element(CellStyle).Text("Result");
                                        header.Cell().Element(CellStyle).Text("Normal Range");
                                        header.Cell().Element(CellStyle).Text("Notes");

                                        static IContainer CellStyle(IContainer container) =>
                                            container.DefaultTextStyle(x => x.SemiBold())
                                                     .PaddingVertical(5)
                                                     .BorderBottom(1)
                                                     .BorderColor(Colors.Black);
                                    });

                                    foreach (var result in group)
                                    {
                                        var testType = result.TestType;
                                        table.Cell().Text(result.TestRequestId.ToString());
                                        table.Cell().Text(testType.TestName);
                                        table.Cell().Text(result.ResultValue ?? "—");
                                        table.Cell().Text(testType.NormalRangeMin.HasValue && testType.NormalRangeMax.HasValue
                                            ? $"{testType.NormalRangeMin} - {testType.NormalRangeMax} {testType.UnitsOfMeasurement}"
                                            : "—");
                                        table.Cell().Text(result.Notes ?? "—");
                                    }
                                });
                            }

                            column.Item().PaddingTop(20).Text($"Total Results: {results.Count}").SemiBold();
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        #endregion
    }




    public class TwoFactorService : ITwoFactorService
    {
        public string GenerateSecretKey()
        {
            var key = KeyGeneration.GenerateRandomKey(20);
            return Base32Encoding.ToString(key);
        }

        public string GetQrCodeUri(string secretKey, string email, string issuer)
        {
            // otpauth://totp/{issuer}:{email}?secret={key}&issuer={issuer}
            var encodedIssuer = Uri.EscapeDataString(issuer);
            var encodedEmail = Uri.EscapeDataString(email);
            return $"otpauth://totp/{encodedIssuer}:{encodedEmail}" +
                   $"?secret={secretKey}&issuer={encodedIssuer}&digits=6&period=30";
        }

        public byte[] GenerateQrCodePng(string uri)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            return qrCode.GetGraphic(6);
        }

        public bool VerifyCode(string secretKey, string code)
        {
            try
            {
                var keyBytes = Base32Encoding.ToBytes(secretKey);
                var totp = new Totp(keyBytes);

                // Allow 1 step of clock drift in each direction
                return totp.VerifyTotp(
                    code.Trim(),
                    out _,
                    new VerificationWindow(previous: 1, future: 1));
            }
            catch
            {
                return false;
            }
        }

        public List<string> GenerateRecoveryCodes()
        {
            var rng = new Random();
            var codes = new List<string>();

            for (int i = 0; i < 8; i++)
            {
                // Format: XXXX-XXXX  (8 hex chars)
                var part1 = rng.Next(0x1000, 0xFFFF).ToString("X4");
                var part2 = rng.Next(0x1000, 0xFFFF).ToString("X4");
                codes.Add($"{part1}-{part2}");
            }

            return codes;
        }

        public bool VerifyRecoveryCode(string storedJson, string inputCode,
                                        out string updatedJson)
        {
            updatedJson = storedJson;

            var codes = JsonSerializer.Deserialize<List<string>>(storedJson)
                        ?? new List<string>();

            // Recovery codes are stored as BCrypt hashes
            var matched = codes.FirstOrDefault(c =>
                BCrypt.Net.BCrypt.Verify(inputCode.Trim().ToUpper(), c));

            if (matched == null) return false;

            // Remove the used code (one-time use)
            codes.Remove(matched);
            updatedJson = JsonSerializer.Serialize(codes);
            return true;
        }
    }



}
