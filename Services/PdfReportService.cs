using LaboratoryTestRequestManagementSystem.AppStatus;
using LaboratoryTestRequestManagementSystem.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LaboratoryTestRequestManagementSystem.Services
{
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
}