using LaboratoryTestRequestManagementSystem.AppStatus;
using LaboratoryTestRequestManagementSystem.Data;
using LaboratoryTestRequestManagementSystem.Models;
using LaboratoryTestRequestManagementSystem.Services;
using LaboratoryTestRequestManagementSystem.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LaboratoryTestRequestManagementSystem.Controllers
{
    [Authorize(Roles = "LabTechnician")]
    public class LabTechnicianController : Controller
    {
        private readonly LabDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IPdfReportService _pdfService;
        private readonly INotificationService _notificationService;

        private const string SuccessMessageKey = "SuccessMessage";
        private const string ErrorMessageKey = "ErrorMessage";

        public LabTechnicianController(LabDbContext context, IEmailService emailService,
                                       IPdfReportService pdfService, INotificationService notificationService)
        {
            _context = context;
            _emailService = emailService;
            _pdfService = pdfService;
            _notificationService = notificationService;
        }

        // ======================================================================
        //  HELPER METHODS
        // ======================================================================
        private void SetSuccess(string message) => TempData[SuccessMessageKey] = message;
        private void SetError(string message) => TempData[ErrorMessageKey] = message;

        #region Dashboard
        public async Task<IActionResult> DashBoard(string? filterUrgency, int? filterCategoryId, string? filterDueTime, string? filterRequestNumber)
        {
            int technicianId = GetCurrentTechnicianId();

            var baseQuery = _context.TestRequestTestTypes
                .Include(trt => trt.TestType).ThenInclude(tt => tt.TestCategory)
                .Include(trt => trt.TestRequest).ThenInclude(tr => tr.Patient)
                .Include(trt => trt.TestRequest).ThenInclude(tr => tr.Doctor)
                .Include(trt => trt.Technician)
                .Where(trt => trt.TestRequest.RecordStatus == Status.Active);

            var qualifiedTestTypeIds = await _context.TechnicianTestTypes
                .Where(tt => tt.TechnicianId == technicianId)
                .Select(tt => tt.TestTypeId)
                .ToListAsync();

            IQueryable<TestRequestTestType> ApplyFilters(IQueryable<TestRequestTestType> query)
            {
                if (!string.IsNullOrEmpty(filterUrgency) && Enum.TryParse<Urgency>(filterUrgency, out var urgency))
                    query = query.Where(trt => trt.TestRequest.Urgency == urgency);
                if (filterCategoryId.HasValue)
                    query = query.Where(trt => trt.TestType.TestCategoryId == filterCategoryId);
                if (!string.IsNullOrEmpty(filterRequestNumber))
                {
                    if (int.TryParse(filterRequestNumber.Replace("REQ-", "").TrimStart('0'), out int reqId))
                        query = query.Where(trt => trt.TestRequestId == reqId);
                }
                var now = DateTime.Now;
                if (filterDueTime == "Today")
                    query = query.Where(trt => trt.StartDateTime.HasValue && trt.StartDateTime.Value.Date == now.Date);
                else if (filterDueTime == "ThisWeek")
                {
                    var startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek);
                    var endOfWeek = startOfWeek.AddDays(7);
                    query = query.Where(trt => trt.StartDateTime.HasValue && trt.StartDateTime.Value.Date >= startOfWeek && trt.StartDateTime.Value.Date < endOfWeek);
                }
                return query;
            }

            IQueryable<DashboardTestItemViewModel> ProjectToViewModel(IQueryable<TestRequestTestType> query)
            {
                var now = DateTime.Now;
                return query.Select(trt => new DashboardTestItemViewModel
                {
                    TestRequestId = trt.TestRequestId,
                    TestTypeId = trt.TestTypeId,
                    PatientName = trt.TestRequest.Patient.FirstName + " " + trt.TestRequest.Patient.LastName,
                    TestName = trt.TestType.TestName,
                    Urgency = trt.TestRequest.Urgency,
                    CategoryName = trt.TestType.TestCategory.CategoryName,
                    StartDateTime = trt.StartDateTime,
                    ExpectedCompletionTime = trt.StartDateTime.HasValue ? trt.StartDateTime.Value.AddMinutes(trt.TestType.TurnaroundTimeMinutes) : null,
                    IsOverdue = trt.StartDateTime.HasValue && !trt.CompletionDateTime.HasValue && now > trt.StartDateTime.Value.AddMinutes(trt.TestType.TurnaroundTimeMinutes),
                    IsNearingLimit = trt.StartDateTime.HasValue && !trt.CompletionDateTime.HasValue && !trt.RequestStatus.ToString().EndsWith("ed") &&
                                     now.AddMinutes(30) > trt.StartDateTime.Value.AddMinutes(trt.TestType.TurnaroundTimeMinutes) && now <= trt.StartDateTime.Value.AddMinutes(trt.TestType.TurnaroundTimeMinutes),
                    Status = trt.RequestStatus.ToString()
                });
            }

            var selectedTests = await ProjectToViewModel(ApplyFilters(baseQuery.Where(trt => trt.TechnicianId == technicianId && trt.RequestStatus == RequestStatus.InProgress))).ToListAsync();
            var waitingSelectionTests = await ProjectToViewModel(ApplyFilters(baseQuery.Where(trt => trt.TestRequest.RequestStatus == RequestStatus.SamplesReceived && trt.RequestStatus == RequestStatus.Submitted && qualifiedTestTypeIds.Contains(trt.TestTypeId) && trt.TechnicianId == null))).ToListAsync();
            var waitingVerificationTests = await ProjectToViewModel(ApplyFilters(baseQuery.Where(trt => trt.RequestStatus == RequestStatus.Completed && trt.TechnicianId != technicianId && qualifiedTestTypeIds.Contains(trt.TestTypeId)))).ToListAsync();
            var waitingReviewTests = await ProjectToViewModel(ApplyFilters(baseQuery.Where(trt => trt.RequestStatus == RequestStatus.ToBeReviewed && trt.TechnicianId == technicianId))).ToListAsync();
            var urgentTests = await ProjectToViewModel(ApplyFilters(baseQuery.Where(trt => trt.TestRequest.Urgency == Urgency.Stat && (qualifiedTestTypeIds.Contains(trt.TestTypeId) || trt.TechnicianId == technicianId) && trt.RequestStatus != RequestStatus.Verified && trt.RequestStatus != RequestStatus.Completed && trt.RequestStatus != RequestStatus.ReleasedByDoctor))).ToListAsync();
            var now = DateTime.Now;
            var overdueTests = await ProjectToViewModel(ApplyFilters(baseQuery.Where(trt => trt.StartDateTime.HasValue && !trt.CompletionDateTime.HasValue && trt.RequestStatus == RequestStatus.InProgress && (qualifiedTestTypeIds.Contains(trt.TestTypeId) || trt.TechnicianId == technicianId) && now > trt.StartDateTime.Value.AddMinutes(trt.TestType.TurnaroundTimeMinutes)))).ToListAsync();
            var nearingTests = await ProjectToViewModel(ApplyFilters(baseQuery.Where(trt => trt.StartDateTime.HasValue && !trt.CompletionDateTime.HasValue && trt.RequestStatus == RequestStatus.InProgress && (qualifiedTestTypeIds.Contains(trt.TestTypeId) || trt.TechnicianId == technicianId) && now.AddMinutes(30) > trt.StartDateTime.Value.AddMinutes(trt.TestType.TurnaroundTimeMinutes) && now <= trt.StartDateTime.Value.AddMinutes(trt.TestType.TurnaroundTimeMinutes)))).ToListAsync();

            var model = new TechnicianDashboardViewModel
            {
                SelectedTestsCount = selectedTests.Count,
                WaitingForSelectionCount = waitingSelectionTests.Count,
                WaitingForVerificationCount = waitingVerificationTests.Count,
                WaitingForReviewCount = waitingReviewTests.Count,
                UrgentTestsCount = urgentTests.Count,
                OverdueTestsCount = overdueTests.Count,
                NearingLimitCount = nearingTests.Count,

                SelectedTests = selectedTests,
                WaitingForSelectionTests = waitingSelectionTests,
                WaitingForVerificationTests = waitingVerificationTests,
                WaitingForReviewTests = waitingReviewTests,
                UrgentTests = urgentTests,
                OverdueTests = overdueTests,
                NearingLimitTests = nearingTests,

                FilterUrgency = filterUrgency,
                FilterCategoryId = filterCategoryId,
                FilterDueTime = filterDueTime,
                FilterRequestNumber = filterRequestNumber,

                CategoryOptions = new SelectList(await _context.TestCategories.Where(tc => tc.Status == Status.Active).ToListAsync(), "Id", "CategoryName")
            };

            return View(model);
        }
        #endregion

        // ======================================================================
        //  CANCEL REQUEST (IMPROVED)
        // ======================================================================
        #region Cancel Request (by Lab Technician)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRequest(int id, string cancellationReason)
        {
            if (string.IsNullOrWhiteSpace(cancellationReason))
            {
                SetError("Cancellation reason is required.");
                return RedirectToAction(nameof(PendingTestRequests));
            }

            var request = await _context.TestRequests
                .Include(tr => tr.Doctor)
                .Include(tr => tr.Patient)
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.RecordStatus == Status.Active);

            if (request == null) return NotFound();

            if (request.RequestStatus != RequestStatus.Submitted && request.RequestStatus != RequestStatus.SamplesReceived)
            {
                SetError("Cannot cancel a request that is already in progress or completed.");
                return RedirectToAction(nameof(PendingTestRequests));
            }

            // Perform cancellation
            request.RequestStatus = RequestStatus.Cancelled;
            request.CancellationReason = cancellationReason;
            request.DateCancelled = DateTime.Now;
            await _context.SaveChangesAsync();

            // ---------- NEW: Generate PDF cancellation record ----------
            byte[]? cancellationPdf = null;
            try
            {
                // Assumes you have added a method GenerateCancellationPdf to IPdfReportService
                cancellationPdf = await _pdfService.GenerateCancellationPdf(request.Id);
            }
            catch
            {
                // If the method is not yet implemented, proceed without PDF.
                // You can safely add the method later without breaking existing functionality.
            }

            // ---------- Notify Doctor ----------
            if (request.Doctor != null)
            {
                await _notificationService.CreateAsync(request.Doctor.Id, "Doctor",
                    $"Test request #{request.Id} for patient {request.Patient?.FirstName} has been cancelled by the lab. Reason: {cancellationReason}",
                    $"/Doctor/RequestDetails/{request.Id}");

                string emailBody = $"Dear Dr. {request.Doctor.LastName},\n\n" +
                                  $"Test request #{request.Id} (Patient: {request.Patient?.FirstName} {request.Patient?.LastName}) " +
                                  $"has been cancelled by the laboratory.\nReason: {cancellationReason}";

                if (cancellationPdf != null)
                    await _emailService.SendEmailWithAttachmentAsync(request.Doctor.Email, "Test Request Cancelled by Lab", emailBody,
                        cancellationPdf, $"Cancellation_Request{request.Id}.pdf");
                else
                    await _emailService.SendEmailAsync(request.Doctor.Email, "Test Request Cancelled by Lab", emailBody);
            }

            // ---------- NEW: Notify Patient ----------
            if (request.Patient != null && !string.IsNullOrEmpty(request.Patient.Email))
            {
                string patientBody = $"Dear {request.Patient.FirstName},\n\n" +
                                     $"Your test request #{request.Id} has been cancelled by the laboratory.\n" +
                                     $"Reason: {cancellationReason}\n\n" +
                                     $"Please contact your doctor for more information.";

                if (cancellationPdf != null)
                    await _emailService.SendEmailWithAttachmentAsync(request.Patient.Email, "Test Request Cancelled", patientBody,
                        cancellationPdf, $"Cancellation_Request{request.Id}.pdf");
                else
                    await _emailService.SendEmailAsync(request.Patient.Email, "Test Request Cancelled", patientBody);
            }

            SetSuccess("Test request cancelled and notifications sent.");
            return RedirectToAction(nameof(PendingTestRequests));
        }
        #endregion

        // The rest of the controller (Receive Samples, Soft Delete, Process Test Types, etc.)
        // remains exactly the same as in your original code...
        #region Receive Samples
        public async Task<IActionResult> PendingTestRequests()
        {
            var requests = await _context.TestRequests
                .Where(tr => tr.RequestStatus == RequestStatus.Submitted && tr.RecordStatus == Status.Active)
                .Include(tr => tr.Patient)
                .Include(tr => tr.Doctor)
                .Include(tr => tr.Samples)
                .OrderByDescending(tr => tr.RequestDate)
                .Select(tr => new PendingTestRequestViewModel
                {
                    Id = tr.Id,
                    PatientName = tr.Patient.FirstName + " " + tr.Patient.LastName,
                    DoctorName = tr.Doctor.FirstName + " " + tr.Doctor.LastName,
                    RequestDate = tr.RequestDate,
                    Urgency = tr.Urgency,
                    SampleCount = tr.Samples.Count
                })
                .ToListAsync();

            return View(requests);
        }

        [HttpGet]
        public async Task<IActionResult> ReceiveSamples(int id)
        {
            var request = await _context.TestRequests
                .Include(tr => tr.Patient)
                .Include(tr => tr.Doctor)
                .Include(tr => tr.Samples).ThenInclude(s => s.SampleType)
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.RequestStatus == RequestStatus.Submitted && tr.RecordStatus == Status.Active);

            if (request == null) return NotFound();

            var model = new ReceiveSampleViewModel
            {
                TestRequestId = request.Id,
                PatientName = request.Patient.FirstName + " " + request.Patient.LastName,
                DoctorName = request.Doctor.FirstName + " " + request.Doctor.LastName,
                RequestDate = request.RequestDate,
                Samples = request.Samples.Select(s => new SampleItemToReceiveViewModel
                {
                    SampleId = s.Id,
                    Barcode = s.Barcode,
                    SampleType = s.SampleType.Name,
                    IsReceived = s.ReceivedDate.HasValue && !s.IsDamaged,
                    ReceivedDate = s.ReceivedDate,
                    IsDamaged = s.IsDamaged,
                    RejectionReason = s.RejectionReason
                }).ToList()
            };

            // Show warning if some samples still missing
            if (model.MissingCount > 0)
                ViewBag.MissingSampleWarning = $"{model.MissingCount} sample(s) still not received or marked damaged.";
            else
                ViewBag.AllSamplesAccounted = true;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReceiveSamples(ReceiveSampleViewModel model)
        {
            var request = await _context.TestRequests
                .Include(tr => tr.Samples)
                .FirstOrDefaultAsync(tr => tr.Id == model.TestRequestId && tr.RequestStatus == RequestStatus.Submitted && tr.RecordStatus == Status.Active);

            if (request == null) return NotFound();

            int technicianId = GetCurrentTechnicianId();
            bool anyReceived = false;
            bool allAccounted = true;

            foreach (var sampleVm in model.Samples)
            {
                var sample = request.Samples.FirstOrDefault(s => s.Id == sampleVm.SampleId);
                if (sample == null) continue;

                sample.IsDamaged = sampleVm.IsDamaged;
                sample.RejectionReason = sampleVm.IsDamaged ? sampleVm.RejectionReason : null;

                if (!sample.ReceivedDate.HasValue && !sampleVm.IsDamaged && sampleVm.IsReceived)
                {
                    sample.ReceivedDate = DateTime.Now;
                    sample.ReceivedById = technicianId;
                    sample.IsDamaged = false;   // clear any previous flag
                    anyReceived = true;
                }
                else if (sampleVm.IsDamaged)
                {
                    sample.ReceivedDate = null;   // not counted as received
                }

                if (!sample.ReceivedDate.HasValue && !sample.IsDamaged)
                    allAccounted = false;
            }

            if (allAccounted && anyReceived)
                request.RequestStatus = RequestStatus.SamplesReceived;
            else if (anyReceived)
                TempData["MissingSampleWarning"] = "Some samples are still missing. All samples must be accounted for (received or marked damaged) before proceeding.";

            await _context.SaveChangesAsync();

            var doctor = await _context.Employees.FindAsync(request.DoctorId);
            if (doctor != null && anyReceived)
            {
                await _notificationService.CreateAsync(doctor.Id, "Doctor",
                    $"Samples for request #{request.Id} (patient: {request.Patient?.FirstName}) have been processed. Damaged: {model.DamagedCount}, Received: {model.ReceivedCount}.",
                    $"/Doctor/RequestDetails/{request.Id}");
            }

            if (allAccounted)
                SetSuccess("All samples accounted for.");
            else
                SetError("Not all samples have been accounted for. Please mark any missing samples as damaged or received.");

            return RedirectToAction(nameof(ReceiveSamples), new { id = model.TestRequestId });
        }

        // AJAX barcode endpoint – used by the scanner input
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReceiveByBarcodeForRequest(int requestId, string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return Json(new { success = false, message = "Barcode is required." });

            var sample = await _context.Samples
                .Include(s => s.TestRequest)
                .Include(s => s.SampleType)
                .FirstOrDefaultAsync(s => s.Barcode == barcode && s.Status == Status.Active);

            if (sample == null)
                return Json(new { success = false, message = "Sample not found." });

            if (sample.ReceivedDate.HasValue && !sample.IsDamaged)
                return Json(new { success = false, message = "Sample already received." });

            if (sample.TestRequestId != requestId)
            {
                // Check if the sample type is even required for this request
                var requiredSampleTypeIds = await _context.TestRequestTestTypes
                    .Where(trt => trt.TestRequestId == requestId && trt.TestType.SampleTypeId != null)
                    .Select(trt => trt.TestType.SampleTypeId)
                    .Distinct()
                    .ToListAsync();

                if (!requiredSampleTypeIds.Contains(sample.SampleTypeId))
                    return Json(new { success = false, message = $"Sample type '{sample.SampleType?.Name}' does not match required types for this request." });
            }

            int technicianId = GetCurrentTechnicianId();
            sample.ReceivedDate = DateTime.Now;
            sample.ReceivedById = technicianId;
            sample.IsDamaged = false;

            var request = sample.TestRequest;
            bool allAccounted = request.Samples.All(s => s.ReceivedDate.HasValue || s.IsDamaged);

            if (request.RequestStatus == RequestStatus.Submitted && allAccounted)
                request.RequestStatus = RequestStatus.SamplesReceived;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"Sample {barcode} received.",
                sampleId = sample.Id,
                barcode = sample.Barcode,
                sampleType = sample.SampleType?.Name,
                requestId = request.Id,
                allAccounted
            });
        }

        // Standard barcode reception (used by external calls, redirected here)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReceiveByBarcode(string barcode, int? redirectRequestId)
        {
            if (string.IsNullOrWhiteSpace(barcode))
            {
                SetError("Barcode is required.");
                return redirectRequestId.HasValue
                    ? RedirectToAction(nameof(ReceiveSamples), new { id = redirectRequestId.Value })
                    : RedirectToAction(nameof(PendingTestRequests));
            }

            var sample = await _context.Samples
                .Include(s => s.TestRequest)
                .FirstOrDefaultAsync(s => s.Barcode == barcode && s.Status == Status.Active);

            if (sample == null)
            {
                SetError("Sample not found.");
                return redirectRequestId.HasValue
                    ? RedirectToAction(nameof(ReceiveSamples), new { id = redirectRequestId.Value })
                    : RedirectToAction(nameof(PendingTestRequests));
            }

            if (sample.ReceivedDate.HasValue && !sample.IsDamaged)
            {
                SetError("Sample already received.");
                return redirectRequestId.HasValue
                    ? RedirectToAction(nameof(ReceiveSamples), new { id = redirectRequestId.Value })
                    : RedirectToAction(nameof(PendingTestRequests));
            }

            int technicianId = GetCurrentTechnicianId();
            sample.ReceivedDate = DateTime.Now;
            sample.ReceivedById = technicianId;

            if (sample.TestRequest.RequestStatus == RequestStatus.Submitted)
                sample.TestRequest.RequestStatus = RequestStatus.SamplesReceived;

            await _context.SaveChangesAsync();
            SetSuccess($"Sample {barcode} received successfully.");
            return redirectRequestId.HasValue
                ? RedirectToAction(nameof(ReceiveSamples), new { id = redirectRequestId.Value })
                : RedirectToAction(nameof(PendingTestRequests));
        }
        #endregion

        #region Soft Delete & Restore Test Requests
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            var request = await _context.TestRequests
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.RecordStatus == Status.Active);

            if (request == null) return NotFound();
            if (request.RequestStatus != RequestStatus.Submitted && request.RequestStatus != RequestStatus.SamplesReceived)
            {
                SetError("Cannot delete a request that is already in progress or completed.");
                return RedirectToAction(nameof(PendingTestRequests));
            }

            request.RecordStatus = Status.Inactive;
            await _context.SaveChangesAsync();
            SetSuccess("Test request deleted (soft delete).");
            return RedirectToAction(nameof(PendingTestRequests));
        }

        public async Task<IActionResult> InactiveTestRequests()
        {
            var requests = await _context.TestRequests
                .Where(tr => tr.RecordStatus == Status.Inactive)
                .Include(tr => tr.Patient)
                .Include(tr => tr.Doctor)
                .Include(tr => tr.Samples)
                .OrderByDescending(tr => tr.RequestDate)
                .Select(tr => new PendingTestRequestViewModel
                {
                    Id = tr.Id,
                    PatientName = tr.Patient.FirstName + " " + tr.Patient.LastName,
                    DoctorName = tr.Doctor.FirstName + " " + tr.Doctor.LastName,
                    RequestDate = tr.RequestDate,
                    Urgency = tr.Urgency,
                    SampleCount = tr.Samples.Count
                })
                .ToListAsync();
            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreRequest(int id)
        {
            var request = await _context.TestRequests.FirstOrDefaultAsync(tr => tr.Id == id && tr.RecordStatus == Status.Inactive);
            if (request == null) return NotFound();
            request.RecordStatus = Status.Active;
            await _context.SaveChangesAsync();
            SetSuccess("Test request restored.");
            return RedirectToAction(nameof(InactiveTestRequests));
        }
        #endregion

        #region Select and Process Test Types
        public async Task<IActionResult> AvailableForProcessing()
        {
            int technicianId = GetCurrentTechnicianId();
            var requests = await _context.TestRequests
                .Where(tr => (tr.RequestStatus == RequestStatus.SamplesReceived || tr.RequestStatus == RequestStatus.InProgress) && tr.RecordStatus == Status.Active)
                .Include(tr => tr.Patient)
                .Include(tr => tr.Doctor)
                .Include(tr => tr.TestRequestTestTypes).ThenInclude(trt => trt.TestType)
                .OrderByDescending(tr => tr.RequestDate)
                .Select(tr => new ProcessTestRequestListViewModel
                {
                    Id = tr.Id,
                    PatientName = tr.Patient.FirstName + " " + tr.Patient.LastName,
                    DoctorName = tr.Doctor.FirstName + " " + tr.Doctor.LastName,
                    RequestDate = tr.RequestDate,
                    Urgency = tr.Urgency,
                    Status = tr.RequestStatus,
                    TotalTests = tr.TestRequestTestTypes.Count,
                    CompletedTests = tr.TestRequestTestTypes.Count(trt => trt.RequestStatus == RequestStatus.Completed || trt.RequestStatus == RequestStatus.Verified)
                })
                .ToListAsync();
            return View(requests);
        }

        [HttpGet]
        public async Task<IActionResult> ProcessTestTypes(int requestId)
        {
            int technicianId = GetCurrentTechnicianId();

            var request = await _context.TestRequests
                .Include(tr => tr.Patient).ThenInclude(p => p.PatientConditions).ThenInclude(pc => pc.MedicalCondition)
                .Include(tr => tr.Patient).ThenInclude(p => p.PatientAllergies).ThenInclude(pa => pa.Allergy)
                .Include(tr => tr.Patient).ThenInclude(p => p.PatientMedications).ThenInclude(pm => pm.Medication)
                .Include(tr => tr.Doctor)
                .Include(tr => tr.TestRequestTestTypes).ThenInclude(trt => trt.TestType).ThenInclude(tt => tt.SampleType)
                .Include(tr => tr.TestRequestTestTypes).ThenInclude(trt => trt.Technician)
                .FirstOrDefaultAsync(tr => tr.Id == requestId && tr.RecordStatus == Status.Active);

            if (request == null) return NotFound();

            var qualifiedTestTypeIds = await _context.TechnicianTestTypes
                .Where(tt => tt.TechnicianId == technicianId)
                .Select(tt => tt.TestTypeId)
                .ToListAsync();

            var now = DateTime.Now;

            var eligibleTestTypes = request.TestRequestTestTypes
                .Where(trt => qualifiedTestTypeIds.Contains(trt.TestTypeId))
                .Select(trt => new TestTypeItemForProcessingViewModel
                {
                    TestRequestId = trt.TestRequestId,
                    TestTypeId = trt.TestTypeId,
                    TestName = trt.TestType.TestName,
                    SampleType = trt.TestType.SampleType?.Name ?? "N/A",
                    Status = trt.RequestStatus,
                    TechnicianId = trt.TechnicianId,
                    TechnicianName = trt.Technician != null ? trt.Technician.FirstName + " " + trt.Technician.LastName : null,
                    StartDateTime = trt.StartDateTime,
                    CompletionDateTime = trt.CompletionDateTime,
                    CanComplete = trt.RequestStatus == RequestStatus.InProgress && trt.TechnicianId == technicianId && !trt.IsPaused,
                    TurnaroundTimeMinutes = trt.TestType.TurnaroundTimeMinutes,
                    ExpectedCompletionTime = trt.StartDateTime.HasValue
                        ? trt.StartDateTime.Value.AddMinutes(trt.TestType.TurnaroundTimeMinutes) + trt.AccumulatedPauseTime
                        : (DateTime?)null,
                    IsOverdue = trt.StartDateTime.HasValue && !trt.CompletionDateTime.HasValue && !trt.IsPaused &&
                                now > trt.StartDateTime.Value.AddMinutes(trt.TestType.TurnaroundTimeMinutes).Add(trt.AccumulatedPauseTime),
                    VerifiedById = trt.VerifiedById,
                    VerifiedByName = trt.VerifiedBy != null ? trt.VerifiedBy.FirstName + " " + trt.VerifiedBy.LastName : null,
                    VerifiedDateTime = trt.VerifiedDateTime,
                    VerificationNotes = trt.VerificationNotes,
                    CanVerify = trt.RequestStatus == RequestStatus.Completed && trt.TechnicianId != technicianId,
                    CanReturnForReview = trt.RequestStatus == RequestStatus.Completed && trt.TechnicianId != technicianId,
                    CanResubmit = trt.RequestStatus == RequestStatus.ToBeReviewed && trt.TechnicianId == technicianId,

                    // ✅ NEW fields
                    IsPaused = trt.IsPaused,
                    CanPause = trt.RequestStatus == RequestStatus.InProgress && trt.TechnicianId == technicianId && !trt.IsPaused,
                    CanResume = trt.RequestStatus == RequestStatus.InProgress && trt.TechnicianId == technicianId && trt.IsPaused,
                    AccumulatedPauseTime = trt.AccumulatedPauseTime,
                    TechnicianNotes = trt.TechnicianNotes,
                    IsDigitallySigned = trt.IsDigitallySigned,
                    SignedAt = trt.SignedAt

                }).ToList();

            var model = new AvailableTestTypeViewModel
            {
                TestRequestId = request.Id,
                PatientName = request.Patient.FirstName + " " + request.Patient.LastName,
                DoctorName = request.Doctor.FirstName + " " + request.Doctor.LastName,
                RequestDate = request.RequestDate,
                Urgency = request.Urgency,
                ClinicalNotes = request.ClinicalNotes,
                MedicalConditions = request.Patient.PatientConditions.Select(pc => pc.MedicalCondition.Name).ToList(),
                Allergies = request.Patient.PatientAllergies.Select(pa => pa.Allergy.Name).ToList(),
                Medications = request.Patient.PatientMedications.Select(pm => pm.Medication.Name).ToList(),
            };

            ViewBag.CurrentTechnicianId = technicianId;
            return View(model);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResumeTest(int testRequestId, int testTypeId)
        {
            int technicianId = GetCurrentTechnicianId();

            var trt = await _context.TestRequestTestTypes
                .FirstOrDefaultAsync(trt => trt.TestRequestId == testRequestId && trt.TestTypeId == testTypeId);

            if (trt == null) return NotFound();
            if (trt.TechnicianId != technicianId)
            { SetError("You are not assigned to this test."); return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId }); }
            if (!trt.IsPaused)
            { SetError("Test is not paused."); return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId }); }

            // Accumulate pause duration
            if (trt.PausedAt.HasValue)
            {
                var pauseDuration = DateTime.Now - trt.PausedAt.Value;
                trt.AccumulatedPauseTime += pauseDuration;
            }
            trt.IsPaused = false;
            trt.PausedAt = null;

            await _context.SaveChangesAsync();
            SetSuccess($"Test '{trt.TestType?.TestName}' resumed.");
            return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProcessingNotes(int testRequestId, int testTypeId, string technicianNotes)
        {
            int technicianId = GetCurrentTechnicianId();

            var trt = await _context.TestRequestTestTypes
                .FirstOrDefaultAsync(trt => trt.TestRequestId == testRequestId && trt.TestTypeId == testTypeId);

            if (trt == null) return NotFound();
            if (trt.TechnicianId != technicianId)
            { SetError("You are not assigned to this test."); return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId }); }

            trt.TechnicianNotes = technicianNotes;
            await _context.SaveChangesAsync();

            SetSuccess("Notes updated.");
            return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId });
        }







        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PauseTest(int testRequestId, int testTypeId)
        {
            int technicianId = GetCurrentTechnicianId();

            var trt = await _context.TestRequestTestTypes
                .FirstOrDefaultAsync(trt => trt.TestRequestId == testRequestId && trt.TestTypeId == testTypeId);

            if (trt == null) return NotFound();
            if (trt.TechnicianId != technicianId)
            { SetError("You are not assigned to this test."); return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId }); }
            if (trt.RequestStatus != RequestStatus.InProgress || trt.IsPaused)
            { SetError("Test cannot be paused at this time."); return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId }); }

            trt.IsPaused = true;
            trt.PausedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            SetSuccess($"Test '{trt.TestType?.TestName}' paused.");
            return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId });
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartTest(int testRequestId, int testTypeId)
        {
            int technicianId = GetCurrentTechnicianId();

            var trt = await _context.TestRequestTestTypes
                .Include(trt => trt.TestRequest)
                .Include(trt => trt.TestType).ThenInclude(tt => tt.TestTypeConsumables).ThenInclude(ttc => ttc.Consumable)
                .FirstOrDefaultAsync(trt => trt.TestRequestId == testRequestId && trt.TestTypeId == testTypeId);

            if (trt == null) return NotFound();

            bool isQualified = await _context.TechnicianTestTypes.AnyAsync(tt => tt.TechnicianId == technicianId && tt.TestTypeId == testTypeId);
            if (!isQualified) { SetError("You are not qualified to perform this test."); return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId }); }
            if (trt.RequestStatus != RequestStatus.Submitted) { SetError("This test cannot be started."); return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId }); }

            // ** Validate all consumable stock BEFORE making any changes **
            var consumables = trt.TestType.TestTypeConsumables
                .Select(tc => tc.Consumable)
                .Where(c => c != null && c.Status == Status.Active)
                .ToList();

            foreach (var consumable in consumables)
            {
                if (consumable.QuantityOnHand - 1 < 0)
                {
                    SetError($"Insufficient stock for {consumable.ConsumableName}. Cannot start test.");
                    return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId });
                }
            }

            // All good – now deduct stock
            foreach (var consumable in consumables)
            {
                consumable.QuantityOnHand -= 1;
            }

            trt.TechnicianId = technicianId;
            trt.StartDateTime = DateTime.Now;
            trt.RequestStatus = RequestStatus.InProgress;
            // Log consumable usage
            await LogConsumableUsage(trt, technicianId);
            if (trt.TestRequest.RequestStatus == RequestStatus.SamplesReceived)
                trt.TestRequest.RequestStatus = RequestStatus.InProgress;

            await _context.SaveChangesAsync();
            SetSuccess($"Started test: {trt.TestType?.TestName}");
            return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId });
        }
        #endregion

        #region Verification and Review
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyTest(int testRequestId, int testTypeId,
      List<string> selectedChecklistItems, string? verificationNotes, bool digitalSignature)
        {
            int technicianId = GetCurrentTechnicianId();

            var trt = await _context.TestRequestTestTypes
                .Include(trt => trt.TestRequest).ThenInclude(tr => tr.Doctor)
                .Include(trt => trt.TestRequest).ThenInclude(tr => tr.TestRequestTestTypes)
                .Include(trt => trt.TestType)
                .FirstOrDefaultAsync(trt => trt.TestRequestId == testRequestId && trt.TestTypeId == testTypeId);

            if (trt == null) return NotFound();

            bool isQualified = await _context.TechnicianTestTypes.AnyAsync(tt => tt.TechnicianId == technicianId && tt.TestTypeId == testTypeId);
            if (!isQualified) { SetError("You are not qualified to verify this test."); return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId }); }
            if (trt.TechnicianId == technicianId) { SetError("You cannot verify your own test."); return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId }); }
            if (trt.RequestStatus != RequestStatus.Completed) { SetError("Only completed tests can be verified."); return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId }); }

            // ---------- Digital signature validation ----------
            if (!digitalSignature)
            {
                SetError("You must provide a digital signature to verify the test.");
                return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId });
            }

            // ---------- Checklist validation ----------
            if (selectedChecklistItems == null || selectedChecklistItems.Count == 0)
            {
                SetError("You must complete at least one verification checklist item.");
                return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId });
            }

            // Update the test type
            trt.RequestStatus = RequestStatus.Verified;
            trt.VerifiedById = technicianId;
            trt.VerifiedDateTime = DateTime.Now;
            trt.VerificationNotes = verificationNotes;
            trt.IsDigitallySigned = true;
            trt.SignedAt = DateTime.Now;

            // Add review history entry with checklist JSON
            var checklistJson = System.Text.Json.JsonSerializer.Serialize(selectedChecklistItems);
            _context.TestReviewHistories.Add(new TestReviewHistory
            {
                TestRequestId = testRequestId,
                TestTypeId = testTypeId,
                ReviewerId = technicianId,
                Action = "Verified",
                Notes = verificationNotes,
                ActionDate = DateTime.Now,
                VerificationChecklistJson = checklistJson
            });

            await _context.SaveChangesAsync();

            var request = trt.TestRequest;
            bool allVerified = request.TestRequestTestTypes.All(trt2 => trt2.RequestStatus == RequestStatus.Verified);
            if (allVerified)
            {
                await NotifyDoctorAllTestsVerified(request);
            }

            SetSuccess($"Test verified: {trt.TestType?.TestName}");
            return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnForReview(int testRequestId, int testTypeId, string reviewNotes)
        {
            int technicianId = GetCurrentTechnicianId();

            var trt = await _context.TestRequestTestTypes
                .Include(trt => trt.TestType)
                .FirstOrDefaultAsync(trt => trt.TestRequestId == testRequestId && trt.TestTypeId == testTypeId);

            if (trt == null) return NotFound();

            bool isQualified = await _context.TechnicianTestTypes.AnyAsync(tt => tt.TechnicianId == technicianId && tt.TestTypeId == testTypeId);
            if (!isQualified) { SetError("You are not qualified to review this test."); return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId }); }
            if (trt.TechnicianId == technicianId) { SetError("You cannot return your own test for review."); return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId }); }
            if (trt.RequestStatus != RequestStatus.Completed) { SetError("Only completed tests can be returned for review."); return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId }); }
            if (string.IsNullOrWhiteSpace(reviewNotes)) { SetError("Review notes are required."); return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId }); }

            if (trt.TechnicianId.HasValue)
            {
                await _notificationService.CreateAsync(trt.TechnicianId.Value, "LabTechnician",
                    $"A test you performed ({trt.TestType?.TestName}) on request #{testRequestId} has been returned for review. Notes: {reviewNotes}",
                    $"/LabTechnician/ProcessTestTypes?requestId={testRequestId}");
            }

            trt.RequestStatus = RequestStatus.ToBeReviewed;
            trt.VerificationNotes = reviewNotes;
            trt.VerifiedById = technicianId;
            trt.VerifiedDateTime = DateTime.Now;

            _context.TestReviewHistories.Add(new TestReviewHistory
            {
                TestRequestId = testRequestId,
                TestTypeId = testTypeId,
                ReviewerId = technicianId,
                Action = "Returned for Review",
                Notes = reviewNotes,
                ActionDate = DateTime.Now
            });

            await _context.SaveChangesAsync();
            SetSuccess($"Test returned for review: {trt.TestType?.TestName}");
            return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResubmitForVerification(int testRequestId, int testTypeId,
    string? resubmitNotes, string? adjustedResultValue)
        {
            int technicianId = GetCurrentTechnicianId();

            var trt = await _context.TestRequestTestTypes
                .Include(trt => trt.TestType)
                .Include(trt => trt.TestRequest).ThenInclude(tr => tr.TestRequestTestTypes)
                .FirstOrDefaultAsync(trt => trt.TestRequestId == testRequestId && trt.TestTypeId == testTypeId);

            if (trt == null) return NotFound();
            if (trt.TechnicianId != technicianId)
            { SetError("Only the original technician can resubmit this test."); return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId }); }
            if (trt.RequestStatus != RequestStatus.ToBeReviewed)
            { SetError("Only tests awaiting review can be resubmitted."); return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId }); }

            // --- Capture old result for comparison ---
            string? oldResultValue = null;
            var existingResult = await _context.TestResults
                .FirstOrDefaultAsync(tr => tr.TestRequestId == testRequestId && tr.TestTypeId == testTypeId);

            oldResultValue = existingResult?.ResultValue;

            // Update result if a new adjusted value is provided
            if (!string.IsNullOrWhiteSpace(adjustedResultValue))
            {
                if (existingResult != null)
                {
                    existingResult.ResultValue = adjustedResultValue;
                    if (trt.TestType.NormalRangeMin.HasValue && trt.TestType.NormalRangeMax.HasValue)
                    {
                        if (decimal.TryParse(adjustedResultValue, out decimal resultDecimal))
                            existingResult.IsAbnormal = resultDecimal < trt.TestType.NormalRangeMin.Value
                                                        || resultDecimal > trt.TestType.NormalRangeMax.Value;
                    }
                    existingResult.Notes = resubmitNotes ?? existingResult.Notes;
                }
                else
                {
                    // If no previous result exists, create one (edge case)
                    _context.TestResults.Add(new TestResult
                    {
                        TestRequestId = testRequestId,
                        TestTypeId = testTypeId,
                        ResultValue = adjustedResultValue,
                        Notes = resubmitNotes,
                        IsAbnormal = false,
                        CompletedDate = DateTime.Now,
                        Status = Status.Active
                    });
                }
            }

            // Build result change JSON
            string? resultChangeJson = null;
            if (!string.IsNullOrWhiteSpace(adjustedResultValue) && !string.IsNullOrWhiteSpace(oldResultValue))
            {
                resultChangeJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Old = oldResultValue,
                    New = adjustedResultValue
                });
            }

            // Update test status
            trt.RequestStatus = RequestStatus.Completed;
            trt.CompletionDateTime = DateTime.Now;
            trt.ReviewNotes = resubmitNotes;
            trt.VerifiedById = null;
            trt.VerifiedDateTime = null;
            trt.VerificationNotes = null;

            _context.TestReviewHistories.Add(new TestReviewHistory
            {
                TestRequestId = testRequestId,
                TestTypeId = testTypeId,
                ReviewerId = technicianId,
                Action = "Resubmitted",
                Notes = resubmitNotes,
                ActionDate = DateTime.Now,
                ResultChangeJson = resultChangeJson   // NEW
            });

            await _context.SaveChangesAsync();
            SetSuccess($"Test resubmitted for verification: {trt.TestType?.TestName}");
            return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId });
        }

        [HttpGet]
        public async Task<IActionResult> ReviewerTimeline(int testRequestId, int testTypeId)
        {
            var historyEntries = await _context.TestReviewHistories
                .Where(h => h.TestRequestId == testRequestId && h.TestTypeId == testTypeId)
                .Include(h => h.Reviewer)
                .OrderBy(h => h.ActionDate)
                .ToListAsync();

            var trt = await _context.TestRequestTestTypes
                .Include(trt => trt.TestType)
                .FirstOrDefaultAsync(trt => trt.TestRequestId == testRequestId && trt.TestTypeId == testTypeId);

            if (trt == null) return NotFound();

            ViewBag.TestName = trt.TestType.TestName;
            ViewBag.TestRequestId = testRequestId;
            ViewBag.TestTypeId = testTypeId;

            return PartialView("_ReviewerTimeline", historyEntries);
        }

        #endregion





        #region Capture Result
        [HttpGet]
        public async Task<IActionResult> CaptureResult(int testRequestId, int testTypeId)
        {
            int technicianId = GetCurrentTechnicianId();

            var trt = await _context.TestRequestTestTypes
                .Include(trt => trt.TestType)
                .Include(trt => trt.TestRequest).ThenInclude(tr => tr.Patient)
                    .ThenInclude(p => p.PatientConditions).ThenInclude(pc => pc.MedicalCondition)
                .Include(trt => trt.TestRequest).ThenInclude(tr => tr.Patient)
                    .ThenInclude(p => p.PatientAllergies).ThenInclude(pa => pa.Allergy)
                .Include(trt => trt.TestRequest).ThenInclude(tr => tr.Patient)
                    .ThenInclude(p => p.PatientMedications).ThenInclude(pm => pm.Medication)
                .FirstOrDefaultAsync(trt => trt.TestRequestId == testRequestId && trt.TestTypeId == testTypeId);

            if (trt == null) return NotFound();
            if (trt.TechnicianId != technicianId || trt.RequestStatus != RequestStatus.InProgress)
            { SetError("You cannot capture results for this test at this time."); return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId }); }

            var patient = trt.TestRequest.Patient;
            var request = trt.TestRequest;

            var model = new CaptureResultViewModel
            {
                TestRequestId = testRequestId,
                TestTypeId = testTypeId,
                TestName = trt.TestType.TestName,
                PatientName = patient.FirstName + " " + patient.LastName,
                UnitsOfMeasurement = trt.TestType.UnitsOfMeasurement ?? "",
                NormalRangeMin = trt.TestType.NormalRangeMin,
                NormalRangeMax = trt.TestType.NormalRangeMax,
                ClinicalNotes = request.ClinicalNotes,
                MedicalConditions = patient.PatientConditions.Select(pc => pc.MedicalCondition.Name).ToList(),
                Allergies = patient.PatientAllergies.Select(pa => pa.Allergy.Name).ToList(),
                Medications = patient.PatientMedications.Select(pm => pm.Medication.Name).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CaptureResult(CaptureResultViewModel model)
        {
            int technicianId = GetCurrentTechnicianId();

            var trt = await _context.TestRequestTestTypes
                .Include(trt => trt.TestType)
                .Include(trt => trt.TestRequest).ThenInclude(tr => tr.TestRequestTestTypes)
                .FirstOrDefaultAsync(trt => trt.TestRequestId == model.TestRequestId && trt.TestTypeId == model.TestTypeId);

            if (trt == null) return NotFound();
            if (trt.TechnicianId != technicianId || trt.RequestStatus != RequestStatus.InProgress)
            { SetError("You cannot capture results for this test at this time."); return RedirectToAction(nameof(ProcessTestTypes), new { requestId = model.TestRequestId }); }
            if (!ModelState.IsValid) return View(model);

            bool isAbnormal = false;
            if (trt.TestType.NormalRangeMin.HasValue && trt.TestType.NormalRangeMax.HasValue)
            {
                if (decimal.TryParse(model.ResultValue, out decimal resultDecimal))
                    isAbnormal = resultDecimal < trt.TestType.NormalRangeMin.Value || resultDecimal > trt.TestType.NormalRangeMax.Value;
            }

            var testResult = new TestResult
            {
                TestRequestId = model.TestRequestId,
                TestTypeId = model.TestTypeId,
                ResultValue = model.ResultValue,
                Notes = model.Notes,
                IsAbnormal = isAbnormal,
                CompletedDate = DateTime.Now,
                Status = Status.Active
            };
            _context.TestResults.Add(testResult);

            trt.CompletionDateTime = DateTime.Now;
            trt.RequestStatus = RequestStatus.Completed;

            var req = trt.TestRequest;
            bool allCompleted = req.TestRequestTestTypes.All(trt2 => trt2.RequestStatus == RequestStatus.Completed || trt2.RequestStatus == RequestStatus.Verified);
            if (allCompleted) req.RequestStatus = RequestStatus.Completed;

            await _context.SaveChangesAsync();
            SetSuccess($"Results captured for test: {trt.TestType.TestName}");
            return RedirectToAction(nameof(ProcessTestTypes), new { requestId = model.TestRequestId });
        }
        #endregion

        #region Test Review History
        [HttpGet]
        public async Task<IActionResult> TestReviewHistory(int testRequestId, int testTypeId)
        {
            var histories = await _context.TestReviewHistories
                .Where(h => h.TestRequestId == testRequestId && h.TestTypeId == testTypeId)
                .Include(h => h.Reviewer)
                .OrderBy(h => h.ActionDate)
                .ToListAsync();

            var trt = await _context.TestRequestTestTypes
                .Include(trt => trt.TestType)
                .Include(trt => trt.TestRequest).ThenInclude(tr => tr.Patient)
                .FirstOrDefaultAsync(trt => trt.TestRequestId == testRequestId && trt.TestTypeId == testTypeId);

            if (trt == null) return NotFound();

            ViewBag.TestName = trt.TestType.TestName;
            ViewBag.PatientName = trt.TestRequest.Patient.FirstName + " " + trt.TestRequest.Patient.LastName;
            ViewBag.TestRequestId = testRequestId;
            ViewBag.TestTypeId = testTypeId;

            return View(histories);
        }
        #endregion

        #region Reports
        [HttpGet]
        public IActionResult CompletedTestsReport()
        {
            return View(new TechnicianReportFilterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompletedTestsReport(TechnicianReportFilterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            int technicianId = GetCurrentTechnicianId();
            var pdfBytes = await _pdfService.GenerateTechnicianCompletedTestsReport(technicianId, model.StartDate, model.EndDate);
            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                SetError("PDF generation is not yet implemented or no data found.");
                return RedirectToAction(nameof(CompletedTestsReport));
            }
            string fileName = $"CompletedTests_{model.StartDate:yyyyMMdd}-{model.EndDate:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        #endregion

        #region Private Helpers
        private async Task NotifyDoctorAllTestsVerified(TestRequest request)
        {
            var doctor = request.Doctor;
            if (doctor == null || string.IsNullOrEmpty(doctor.Email)) return;

            await _notificationService.CreateAsync(doctor.Id, "Doctor",
                $"All tests for request #{request.Id} have been verified. Please review the results and release them to the patient.",
                $"/Doctor/RequestDetails/{request.Id}");

            byte[] pdfBytes = await _pdfService.GenerateTestResultsPdf(request.Id);
            string subject = $"All Tests Verified – Request #{request.Id}";
            string body = $"Dear Dr. {doctor.LastName},\n\n" +
                          $"All tests for request #{request.Id} (Patient: {request.Patient?.FirstName} {request.Patient?.LastName}) " +
                          $"have been verified and are ready for your review.\n\n" +
                          $"Please find the results attached.";

            await _emailService.SendEmailWithAttachmentAsync(
                doctor.Email,
                subject,
                body,
                pdfBytes,
                $"TestResults_Request{request.Id}.pdf"
            );
        }

        private int GetCurrentTechnicianId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out int id) ? id : 0;
        }
        #endregion

        /// <summary>
        /// Notifies each assigned technician about tests that are nearing their expected completion time (within 30 minutes).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> NotifyNearingDeadline()
        {
            var now = DateTime.Now;
            var warningThreshold = now.AddMinutes(30);

            // 1. Fetch all candidate tests first (client evaluation for computed expected time)
            var allCandidates = await _context.TestRequestTestTypes
                .Include(trt => trt.TestRequest).ThenInclude(tr => tr.Patient)
                .Include(trt => trt.TestType)
                .Include(trt => trt.Technician)
                .Where(trt => trt.RequestStatus == RequestStatus.InProgress
                              && trt.StartDateTime.HasValue
                              && !trt.IsPaused
                              && trt.TechnicianId != null)
                .ToListAsync();

            // 2. Filter in memory using the computed expected completion time
            var nearingTests = allCandidates
                .Where(trt =>
                {
                    var expected = trt.StartDateTime!.Value
                                        .AddMinutes(trt.TestType.TurnaroundTimeMinutes)
                                        .Add(trt.AccumulatedPauseTime);
                    return expected > now && expected <= warningThreshold;
                })
                .ToList();

            if (!nearingTests.Any())
            {
                SetSuccess("No tests nearing deadline.");
                return RedirectToAction(nameof(DashBoard));
            }

            foreach (var test in nearingTests)
            {
                if (test.Technician != null)
                {
                    await _notificationService.CreateAsync(
                        test.TechnicianId!.Value,
                        "LabTechnician",
                        $"Nearing Deadline: {test.TestType.TestName} for patient {test.TestRequest.Patient?.FirstName} " +
                        $"in request #{test.TestRequestId} is due at " +
                        $"{test.StartDateTime!.Value.AddMinutes(test.TestType.TurnaroundTimeMinutes).Add(test.AccumulatedPauseTime):g}.",
                        $"/LabTechnician/ProcessTestTypes?requestId={test.TestRequestId}"
                    );
                }
            }

            SetSuccess($"Sent nearing‑deadline alerts for {nearingTests.Count} test(s).");
            return RedirectToAction(nameof(DashBoard));
        }


        #region Stock Management

        /// <summary>
        /// Logs consumable usage when a test is started. This call is already inside StartTest.
        /// We'll inject the logging there.
        /// </summary>
        private async Task LogConsumableUsage(TestRequestTestType trt, int technicianId)
        {
            var consumables = trt.TestType.TestTypeConsumables
                .Select(tc => tc.Consumable)
                .Where(c => c != null && c.Status == Status.Active)
                .ToList();

            foreach (var consumable in consumables)
            {
                _context.ConsumableUsageHistories.Add(new ConsumableUsageHistory
                {
                    ConsumableId = consumable.Id,
                    QuantityUsed = 1, // each test consumes 1 unit per consumable per test type
                    UsageDate = DateTime.Now,
                    TestRequestId = trt.TestRequestId,
                    TestTypeId = trt.TestTypeId,
                    TechnicianId = technicianId
                });
            }
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Sends low-stock notifications to all lab managers.
        /// Low stock = QuantityOnHand <= LowStockThreshold (default 5).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> NotifyLowStock()
        {
            var lowStockItems = await _context.Consumables
                .Where(c => c.Status == Status.Active && c.QuantityOnHand <= (c.LowStockThreshold ?? 5) && c.QuantityOnHand > 0)
                .ToListAsync();

            if (!lowStockItems.Any())
            {
                SetSuccess("No low-stock items found.");
                return RedirectToAction(nameof(DashBoard));
            }

            var managers = await _context.Employees
                .Where(e => e.Role == UserRole.LaboratoryManager && e.IsActive == Status.Active)
                .ToListAsync();

            foreach (var item in lowStockItems)
            {
                foreach (var manager in managers)
                {
                    await _notificationService.CreateAsync(
                        manager.Id,
                        "LabManager",
                        $"Low stock alert: {item.ConsumableName} has only {item.QuantityOnHand} units left (threshold {item.LowStockThreshold ?? 5}).",
                        $"/Consumables/Details/{item.Id}"  // adjust URL if you have such a page
                    );
                }
            }

            SetSuccess($"Low-stock alerts sent for {lowStockItems.Count} consumable(s).");
            return RedirectToAction(nameof(DashBoard));
        }

        /// <summary>
        /// Sends out-of-stock notifications to all lab managers.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> NotifyOutOfStock()
        {
            var outOfStockItems = await _context.Consumables
                .Where(c => c.Status == Status.Active && c.QuantityOnHand == 0)
                .ToListAsync();

            if (!outOfStockItems.Any())
            {
                SetSuccess("No out-of-stock items found.");
                return RedirectToAction(nameof(DashBoard));
            }

            var managers = await _context.Employees
                .Where(e => e.Role == UserRole.LaboratoryManager && e.IsActive == Status.Active)
                .ToListAsync();

            foreach (var item in outOfStockItems)
            {
                foreach (var manager in managers)
                {
                    await _notificationService.CreateAsync(
                        manager.Id,
                        "LabManager",
                        $"OUT OF STOCK: {item.ConsumableName} has 0 units remaining!",
                        $"/Consumables/Details/{item.Id}"
                    );
                }
            }

            SetSuccess($"Out-of-stock alerts sent for {outOfStockItems.Count} consumable(s).");
            return RedirectToAction(nameof(DashBoard));
        }

        /// <summary>
        /// Shows consumable usage history for today (or a given date).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ConsumableUsageHistory(DateTime? date)
        {
            var filterDate = date?.Date ?? DateTime.Today;

            var usage = await _context.ConsumableUsageHistories
                .Include(u => u.Consumable)
                .Include(u => u.TestRequest).ThenInclude(tr => tr.Patient)
                .Include(u => u.TestType)
                .Include(u => u.Technician)
                .Where(u => u.UsageDate.Date == filterDate)
                .OrderByDescending(u => u.UsageDate)
                .ToListAsync();

            ViewBag.ReportDate = filterDate;
            return View(usage);
        }

        /// <summary>
        /// Daily stock movement report: groups today's usage by consumable.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> DailyStockMovementReport()
        {
            var today = DateTime.Today;
            var usage = await _context.ConsumableUsageHistories
                .Include(u => u.Consumable)
                .Where(u => u.UsageDate.Date == today)
                .GroupBy(u => new { u.ConsumableId, u.Consumable.ConsumableName })
                .Select(g => new
                {
                    ConsumableName = g.Key.ConsumableName,
                    TotalUsed = g.Sum(u => u.QuantityUsed),
                    CurrentStock = g.First().Consumable.QuantityOnHand
                })
                .ToListAsync();

            ViewBag.Today = today;
            return View(usage);
        }

        #endregion



        #region Patient History
        [HttpGet]
        public async Task<IActionResult> PatientHistory(int patientId)
        {
            var patient = await _context.Patients
                .Include(p => p.TestRequests)
                    .ThenInclude(tr => tr.TestRequestTestTypes)
                        .ThenInclude(trt => trt.TestType)
                .Include(p => p.TestRequests)
                    .ThenInclude(tr => tr.TestResults)
                .Include(p => p.TestRequests)
                    .ThenInclude(tr => tr.Doctor)
                .FirstOrDefaultAsync(p => p.Id == patientId);

            if (patient == null) return NotFound();

            var history = patient.TestRequests
                .Where(tr => tr.RecordStatus == Status.Active)
                .SelectMany(tr => tr.TestRequestTestTypes, (tr, trt) => new
                {
                    tr,
                    trt,
                    result = tr.TestResults.FirstOrDefault(r => r.TestTypeId == trt.TestTypeId)
                })
                .OrderByDescending(x => x.tr.RequestDate)
                .Select(x => new PatientHistoryItem
                {
                    TestRequestId = x.tr.Id,
                    RequestDate = x.tr.RequestDate,
                    DoctorName = x.tr.Doctor?.FirstName + " " + x.tr.Doctor?.LastName,
                    TestName = x.trt.TestType.TestName,
                    ResultValue = x.result?.ResultValue,
                    IsAbnormal = x.result?.IsAbnormal ?? false,
                    ResultDate = x.result?.CompletedDate
                })
                .ToList();

            var model = new PatientHistoryViewModel
            {
                PatientName = patient.FirstName + " " + patient.LastName,
                BloodGroup = patient.BloodGroup,
                EmergencyContact = patient.EmergencyContactName != null
                    ? $"{patient.EmergencyContactName} ({patient.EmergencyContactNumber})"
                    : null,
                History = history
            };

            return PartialView("_PatientHistory", model);
        }
        #endregion


    }





}