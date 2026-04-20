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

        public LabTechnicianController(LabDbContext context, IEmailService emailService, IPdfReportService pdfService)
        {
            _context = context;
            _emailService = emailService;
            _pdfService = pdfService;
        }

        public async Task<IActionResult> DashBoard(string? filterUrgency, int? filterCategoryId, string? filterDueTime, string? filterRequestNumber)
        {
            int technicianId = GetCurrentTechnicianId();

            // Base query for tests where technician is involved (qualified, assigned, or verifier)
            var baseQuery = _context.TestRequestTestTypes
                .Include(trt => trt.TestType).ThenInclude(tt => tt.TestCategory)
                .Include(trt => trt.TestRequest).ThenInclude(tr => tr.Patient)
                .Include(trt => trt.TestRequest).ThenInclude(tr => tr.Doctor)
                .Include(trt => trt.Technician)
                .Where(trt => trt.TestRequest.RecordStatus == Status.Active);

            // Get qualified test type IDs for this technician
            var qualifiedTestTypeIds = await _context.TechnicianTestTypes
                .Where(tt => tt.TechnicianId == technicianId)
                .Select(tt => tt.TestTypeId)
                .ToListAsync();

            // Apply filters to all queries
            IQueryable<TestRequestTestType> ApplyFilters(IQueryable<TestRequestTestType> query)
            {
                if (!string.IsNullOrEmpty(filterUrgency) && Enum.TryParse<Urgency>(filterUrgency, out var urgency))
                    query = query.Where(trt => trt.TestRequest.Urgency == urgency);

                if (filterCategoryId.HasValue)
                    query = query.Where(trt => trt.TestType.TestCategoryId == filterCategoryId);

                if (!string.IsNullOrEmpty(filterRequestNumber))
                {
                    // RequestNumber format: REQ-000123
                    if (int.TryParse(filterRequestNumber.Replace("REQ-", "").TrimStart('0'), out int reqId))
                        query = query.Where(trt => trt.TestRequestId == reqId);
                }

                // Due time filter
                var now = DateTime.Now;
                if (filterDueTime == "Today")
                    query = query.Where(trt => trt.StartDateTime.HasValue && trt.StartDateTime.Value.Date == now.Date);
                else if (filterDueTime == "ThisWeek")
                {
                    var startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek);
                    var endOfWeek = startOfWeek.AddDays(7);
                    query = query.Where(trt => trt.StartDateTime.HasValue && trt.StartDateTime.Value.Date >= startOfWeek && trt.StartDateTime.Value.Date < endOfWeek);
                }
                // "Overdue" and "Nearing" are handled in specific queries below

                return query;
            }

            // Helper to project to DashboardTestItemViewModel
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

            // 1. Tests selected by technician (InProgress and assigned to this tech)
            var selectedQuery = baseQuery.Where(trt => trt.TechnicianId == technicianId && trt.RequestStatus == RequestStatus.InProgress);
            selectedQuery = ApplyFilters(selectedQuery);
            var selectedTests = await ProjectToViewModel(selectedQuery).ToListAsync();

            // 2. Tests waiting to be selected (Submitted, samples received, tech qualified, not started)
            var waitingSelectionQuery = baseQuery.Where(trt => trt.TestRequest.RequestStatus == RequestStatus.SamplesReceived
                                                               && trt.RequestStatus == RequestStatus.Submitted
                                                               && qualifiedTestTypeIds.Contains(trt.TestTypeId)
                                                               && trt.TechnicianId == null);
            waitingSelectionQuery = ApplyFilters(waitingSelectionQuery);
            var waitingSelectionTests = await ProjectToViewModel(waitingSelectionQuery).ToListAsync();

            // 3. Tests waiting to be verified (Completed by another tech, this tech qualified to verify)
            var waitingVerificationQuery = baseQuery.Where(trt => trt.RequestStatus == RequestStatus.Completed
                                                                  && trt.TechnicianId != technicianId
                                                                  && qualifiedTestTypeIds.Contains(trt.TestTypeId));
            waitingVerificationQuery = ApplyFilters(waitingVerificationQuery);
            var waitingVerificationTests = await ProjectToViewModel(waitingVerificationQuery).ToListAsync();

            // 4. Tests waiting to be reviewed (ToBeReviewed and original tech is this tech)
            var waitingReviewQuery = baseQuery.Where(trt => trt.RequestStatus == RequestStatus.ToBeReviewed
                                                            && trt.TechnicianId == technicianId);
            waitingReviewQuery = ApplyFilters(waitingReviewQuery);
            var waitingReviewTests = await ProjectToViewModel(waitingReviewQuery).ToListAsync();

            // 5. Urgent tests (STAT) across all categories the tech can see (qualified or assigned)
            var urgentQuery = baseQuery.Where(trt => trt.TestRequest.Urgency == Urgency.Stat
                                                     && (qualifiedTestTypeIds.Contains(trt.TestTypeId) || trt.TechnicianId == technicianId)
                                                     && trt.RequestStatus != RequestStatus.Verified && trt.RequestStatus != RequestStatus.Completed && trt.RequestStatus != RequestStatus.ReleasedByDoctor);
            urgentQuery = ApplyFilters(urgentQuery);
            var urgentTests = await ProjectToViewModel(urgentQuery).ToListAsync();

            // 6. Overdue tests (started, not completed, past expected completion)
            var now = DateTime.Now;
            var overdueQuery = baseQuery.Where(trt => trt.StartDateTime.HasValue
                                                      && !trt.CompletionDateTime.HasValue
                                                      && trt.RequestStatus == RequestStatus.InProgress
                                                      && (qualifiedTestTypeIds.Contains(trt.TestTypeId) || trt.TechnicianId == technicianId)
                                                      && now > trt.StartDateTime.Value.AddMinutes(trt.TestType.TurnaroundTimeMinutes));
            overdueQuery = ApplyFilters(overdueQuery);
            var overdueTests = await ProjectToViewModel(overdueQuery).ToListAsync();

            // 7. Tests nearing turnaround limit (within 30 minutes of expected completion, not overdue)
            var nearingQuery = baseQuery.Where(trt => trt.StartDateTime.HasValue
                                                      && !trt.CompletionDateTime.HasValue
                                                      && trt.RequestStatus == RequestStatus.InProgress
                                                      && (qualifiedTestTypeIds.Contains(trt.TestTypeId) || trt.TechnicianId == technicianId)
                                                      && now.AddMinutes(30) > trt.StartDateTime.Value.AddMinutes(trt.TestType.TurnaroundTimeMinutes)
                                                      && now <= trt.StartDateTime.Value.AddMinutes(trt.TestType.TurnaroundTimeMinutes));
            nearingQuery = ApplyFilters(nearingQuery);
            var nearingTests = await ProjectToViewModel(nearingQuery).ToListAsync();

            // Build ViewModel
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




        #region Receive Samples

        // List test requests with status 'Submitted' (samples not yet received)
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

        // GET: Receive samples for a specific test request
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
                    IsReceived = s.ReceivedDate.HasValue,
                    ReceivedDate = s.ReceivedDate
                }).ToList()
            };

            return View(model);
        }

        // POST: Confirm receipt of samples
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReceiveSamples(ReceiveSampleViewModel model)
        {
            var request = await _context.TestRequests
                .Include(tr => tr.Samples)
                .FirstOrDefaultAsync(tr => tr.Id == model.TestRequestId && tr.RequestStatus == RequestStatus.Submitted && tr.RecordStatus == Status.Active);

            if (request == null) return NotFound();

            int technicianId = GetCurrentTechnicianId();

            // Mark selected samples as received
            foreach (var sampleVm in model.Samples.Where(s => !s.IsReceived))
            {
                var sample = request.Samples.FirstOrDefault(s => s.Id == sampleVm.SampleId);
                if (sample != null && !sample.ReceivedDate.HasValue)
                {
                    sample.ReceivedDate = DateTime.Now;
                    sample.ReceivedById = technicianId;
                }
            }

            // Update test request status to SamplesReceived (if not already)
            if (request.RequestStatus == RequestStatus.Submitted)
            {
                request.RequestStatus = RequestStatus.SamplesReceived;
            }

            await _context.SaveChangesAsync();

            TempData["Message"] = "Samples received successfully.";
            return RedirectToAction(nameof(PendingTestRequests));
        }

        // Quick scan by barcode (optional)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReceiveByBarcode(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
            {
                TempData["Error"] = "Barcode is required.";
                return RedirectToAction(nameof(PendingTestRequests));
            }

            var sample = await _context.Samples
                .Include(s => s.TestRequest)
                .FirstOrDefaultAsync(s => s.Barcode == barcode && s.Status == Status.Active);

            if (sample == null)
            {
                TempData["Error"] = "Sample not found.";
                return RedirectToAction(nameof(PendingTestRequests));
            }

            if (sample.ReceivedDate.HasValue)
            {
                TempData["Error"] = "Sample already received.";
                return RedirectToAction(nameof(PendingTestRequests));
            }

            int technicianId = GetCurrentTechnicianId();
            sample.ReceivedDate = DateTime.Now;
            sample.ReceivedById = technicianId;

            // Update test request status if needed
            if (sample.TestRequest.RequestStatus == RequestStatus.Submitted)
            {
                sample.TestRequest.RequestStatus = RequestStatus.SamplesReceived;
            }

            await _context.SaveChangesAsync();

            TempData["Message"] = $"Sample {barcode} received successfully.";
            return RedirectToAction(nameof(PendingTestRequests));
        }





        #endregion




        #region Soft Delete & Restore Test Requests

        // Soft delete a test request (only allowed if status is Submitted or SamplesReceived)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            var request = await _context.TestRequests
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.RecordStatus == Status.Active);

            if (request == null) return NotFound();

            // Optional: restrict deletion to certain statuses
            if (request.RequestStatus != RequestStatus.Submitted && request.RequestStatus != RequestStatus.SamplesReceived)
            {
                TempData["Error"] = "Cannot delete a request that is already in progress or completed.";
                return RedirectToAction(nameof(PendingTestRequests));
            }

            request.RecordStatus = Status.Inactive;
            await _context.SaveChangesAsync();

            TempData["Message"] = "Test request deleted (soft delete).";
            return RedirectToAction(nameof(PendingTestRequests));
        }

        // List inactive (soft deleted) test requests
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

        // Restore a soft-deleted test request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreRequest(int id)
        {
            var request = await _context.TestRequests
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.RecordStatus == Status.Inactive);

            if (request == null) return NotFound();

            request.RecordStatus = Status.Active;
            await _context.SaveChangesAsync();

            TempData["Message"] = "Test request restored.";
            return RedirectToAction(nameof(InactiveTestRequests));
        }

        #endregion


        #region Select and Process Test Types

        // List test requests available for processing (SamplesReceived or InProgress)
        public async Task<IActionResult> AvailableForProcessing()
        {
            int technicianId = GetCurrentTechnicianId();

            var requests = await _context.TestRequests
                .Where(tr => (tr.RequestStatus == RequestStatus.SamplesReceived || tr.RequestStatus == RequestStatus.InProgress)
                             && tr.RecordStatus == Status.Active)
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

        // View test types for a specific request that the technician can process
        [HttpGet]
        public async Task<IActionResult> ProcessTestTypes(int requestId)
        {
            int technicianId = GetCurrentTechnicianId();

            var request = await _context.TestRequests
                .Include(tr => tr.Patient)
                    .ThenInclude(p => p.PatientConditions).ThenInclude(pc => pc.MedicalCondition)
                .Include(tr => tr.Patient)
                    .ThenInclude(p => p.PatientAllergies).ThenInclude(pa => pa.Allergy)
                .Include(tr => tr.Patient)
                    .ThenInclude(p => p.PatientMedications).ThenInclude(pm => pm.Medication)
                .Include(tr => tr.Doctor)
                .Include(tr => tr.TestRequestTestTypes).ThenInclude(trt => trt.TestType).ThenInclude(tt => tt.SampleType)
                .Include(tr => tr.TestRequestTestTypes).ThenInclude(trt => trt.Technician)
                .FirstOrDefaultAsync(tr => tr.Id == requestId && tr.RecordStatus == Status.Active);

            if (request == null) return NotFound();

            var qualifiedTestTypeIds = await _context.TechnicianTestTypes
                .Where(tt => tt.TechnicianId == technicianId)
                .Select(tt => tt.TestTypeId)
                .ToListAsync();

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
                    CanComplete = trt.RequestStatus == RequestStatus.InProgress && trt.TechnicianId == technicianId,

                    TurnaroundTimeMinutes = trt.TestType.TurnaroundTimeMinutes,
                    ExpectedCompletionTime = trt.StartDateTime.HasValue
                        ? trt.StartDateTime.Value.AddMinutes(trt.TestType.TurnaroundTimeMinutes)
                        : (DateTime?)null,
                    IsOverdue = trt.StartDateTime.HasValue && !trt.CompletionDateTime.HasValue
                        && DateTime.Now > trt.StartDateTime.Value.AddMinutes(trt.TestType.TurnaroundTimeMinutes),

                    VerifiedById = trt.VerifiedById,
                    VerifiedByName = trt.VerifiedBy != null ? trt.VerifiedBy.FirstName + " " + trt.VerifiedBy.LastName : null,
                    VerifiedDateTime = trt.VerifiedDateTime,
                    VerificationNotes = trt.VerificationNotes,
                    CanVerify = trt.RequestStatus == RequestStatus.Completed && trt.TechnicianId != technicianId,
                    CanReturnForReview = trt.RequestStatus == RequestStatus.Completed && trt.TechnicianId != technicianId,
                    CanResubmit = trt.RequestStatus == RequestStatus.ToBeReviewed && trt.TechnicianId == technicianId
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
                TestTypes = eligibleTestTypes
            };

            ViewBag.CurrentTechnicianId = technicianId;
            return View(model);
        }



        // Start a test (assign to current technician, set start time, update statuses)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartTest(int testRequestId, int testTypeId)
        {
            int technicianId = GetCurrentTechnicianId();

            var testRequestTestType = await _context.TestRequestTestTypes
                .Include(trt => trt.TestRequest)
                .Include(trt => trt.TestType).ThenInclude(tt => tt.TestTypeConsumables).ThenInclude(ttc => ttc.Consumable)
                .FirstOrDefaultAsync(trt => trt.TestRequestId == testRequestId && trt.TestTypeId == testTypeId);

            if (testRequestTestType == null) return NotFound();

            // Verify technician is qualified
            bool isQualified = await _context.TechnicianTestTypes
                .AnyAsync(tt => tt.TechnicianId == technicianId && tt.TestTypeId == testTypeId);

            if (!isQualified)
            {
                TempData["Error"] = "You are not qualified to perform this test.";
                return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId });
            }

            // Can only start if status is Submitted
            if (testRequestTestType.RequestStatus != RequestStatus.Submitted)
            {
                TempData["Error"] = "This test cannot be started.";
                return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId });
            }

            // ✅ Deduct consumables from stock
            foreach (var testConsumable in testRequestTestType.TestType.TestTypeConsumables)
            {
                var consumable = testConsumable.Consumable;
                if (consumable != null && consumable.Status == Status.Active)
                {
                    consumable.QuantityOnHand -= 1; // Deduct one unit per consumable used
                    if (consumable.QuantityOnHand < 0)
                    {
                        TempData["Error"] = $"Insufficient stock for {consumable.ConsumableName}. Cannot start test.";
                        return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId });
                    }
                }
            }

            testRequestTestType.TechnicianId = technicianId;
            testRequestTestType.StartDateTime = DateTime.Now;
            testRequestTestType.RequestStatus = RequestStatus.InProgress;

            // Update test request status if this is the first test started
            if (testRequestTestType.TestRequest.RequestStatus == RequestStatus.SamplesReceived)
            {
                testRequestTestType.TestRequest.RequestStatus = RequestStatus.InProgress;
            }

            await _context.SaveChangesAsync();

            TempData["Message"] = $"Started test: {testRequestTestType.TestType?.TestName}";
            return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId });
        }
        // Complete a test (set completion time, update statuses)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteTest(int testRequestId, int testTypeId)
        {
            int technicianId = GetCurrentTechnicianId();

            var testRequestTestType = await _context.TestRequestTestTypes
                .Include(trt => trt.TestRequest).ThenInclude(tr => tr.TestRequestTestTypes)
                .FirstOrDefaultAsync(trt => trt.TestRequestId == testRequestId && trt.TestTypeId == testTypeId);

            if (testRequestTestType == null) return NotFound();

            // Can only complete if assigned to current technician and status is InProgress
            if (testRequestTestType.TechnicianId != technicianId || testRequestTestType.RequestStatus != RequestStatus.InProgress)
            {
                TempData["Error"] = "You cannot complete this test.";
                return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId });
            }

            testRequestTestType.CompletionDateTime = DateTime.Now;
            testRequestTestType.RequestStatus = RequestStatus.Completed;

            // Check if all tests in the request are completed
            var request = testRequestTestType.TestRequest;
            bool allCompleted = request.TestRequestTestTypes.All(trt => trt.RequestStatus == RequestStatus.Completed || trt.RequestStatus == RequestStatus.Verified);

            if (allCompleted)
            {
                request.RequestStatus = RequestStatus.Completed;
                // Notify doctor? (optional)
            }

            await _context.SaveChangesAsync();

            TempData["Message"] = $"Completed test: {testRequestTestType.TestType?.TestName}";
            return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId });
        }

        #endregion



        // Verify a completed test (by a different technician)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyTest(int testRequestId, int testTypeId, string? verificationNotes)
        {
            int technicianId = GetCurrentTechnicianId();

            var testRequestTestType = await _context.TestRequestTestTypes
                .Include(trt => trt.TestRequest).ThenInclude(tr => tr.Doctor)
                .Include(trt => trt.TestRequest).ThenInclude(tr => tr.TestRequestTestTypes)
                .Include(trt => trt.TestType)
                .FirstOrDefaultAsync(trt => trt.TestRequestId == testRequestId && trt.TestTypeId == testTypeId);

            if (testRequestTestType == null) return NotFound();

            // Verify technician is qualified
            bool isQualified = await _context.TechnicianTestTypes
                .AnyAsync(tt => tt.TechnicianId == technicianId && tt.TestTypeId == testTypeId);

            if (!isQualified)
            {
                TempData["Error"] = "You are not qualified to verify this test.";
                return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId });
            }

            // Cannot verify own test
            if (testRequestTestType.TechnicianId == technicianId)
            {
                TempData["Error"] = "You cannot verify your own test.";
                return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId });
            }

            // Can only verify if status is Completed
            if (testRequestTestType.RequestStatus != RequestStatus.Completed)
            {
                TempData["Error"] = "Only completed tests can be verified.";
                return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId });
            }

            testRequestTestType.RequestStatus = RequestStatus.Verified;
            testRequestTestType.VerifiedById = technicianId;
            testRequestTestType.VerifiedDateTime = DateTime.Now;
            testRequestTestType.VerificationNotes = verificationNotes;

            // Log review history
            _context.TestReviewHistories.Add(new TestReviewHistory
            {
                TestRequestId = testRequestId,
                TestTypeId = testTypeId,
                ReviewerId = technicianId,
                Action = "Verified",
                Notes = verificationNotes
            });

            await _context.SaveChangesAsync();

            // Check if all tests on the request are verified
            var request = testRequestTestType.TestRequest;
            bool allVerified = request.TestRequestTestTypes.All(trt => trt.RequestStatus == RequestStatus.Verified);

            if (allVerified)
            {
                // Notify doctor via email with PDF attachment
                await NotifyDoctorAllTestsVerified(request);
            }

            TempData["Message"] = $"Test verified: {testRequestTestType.TestType?.TestName}";
            return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId });
        }

        // Return a test for review (with notes)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnForReview(int testRequestId, int testTypeId, string reviewNotes)
        {
            int technicianId = GetCurrentTechnicianId();

            var testRequestTestType = await _context.TestRequestTestTypes
                .Include(trt => trt.TestType)
                .FirstOrDefaultAsync(trt => trt.TestRequestId == testRequestId && trt.TestTypeId == testTypeId);

            if (testRequestTestType == null) return NotFound();

            // Verify technician is qualified
            bool isQualified = await _context.TechnicianTestTypes
                .AnyAsync(tt => tt.TechnicianId == technicianId && tt.TestTypeId == testTypeId);

            if (!isQualified)
            {
                TempData["Error"] = "You are not qualified to review this test.";
                return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId });
            }

            // Cannot review own test
            if (testRequestTestType.TechnicianId == technicianId)
            {
                TempData["Error"] = "You cannot return your own test for review.";
                return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId });
            }

            // Can only return if status is Completed
            if (testRequestTestType.RequestStatus != RequestStatus.Completed)
            {
                TempData["Error"] = "Only completed tests can be returned for review.";
                return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId });
            }

            if (string.IsNullOrWhiteSpace(reviewNotes))
            {
                TempData["Error"] = "Review notes are required.";
                return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId });
            }

            testRequestTestType.RequestStatus = RequestStatus.ToBeReviewed;
            testRequestTestType.VerificationNotes = reviewNotes; // Store review notes
            testRequestTestType.VerifiedById = technicianId;
            testRequestTestType.VerifiedDateTime = DateTime.Now;

            // Log review history
            _context.TestReviewHistories.Add(new TestReviewHistory
            {
                TestRequestId = testRequestId,
                TestTypeId = testTypeId,
                ReviewerId = technicianId,
                Action = "Returned for Review",
                Notes = reviewNotes
            });

            await _context.SaveChangesAsync();

            TempData["Message"] = $"Test returned for review: {testRequestTestType.TestType?.TestName}";
            return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId });
        }

        // Resubmit a test after review (by original technician)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResubmitForVerification(int testRequestId, int testTypeId, string? resubmitNotes, string? adjustedResultValue)
        {
            int technicianId = GetCurrentTechnicianId();

            var testRequestTestType = await _context.TestRequestTestTypes
                .Include(trt => trt.TestType)
                .Include(trt => trt.TestRequest).ThenInclude(tr => tr.TestRequestTestTypes)
                .FirstOrDefaultAsync(trt => trt.TestRequestId == testRequestId && trt.TestTypeId == testTypeId);

            if (testRequestTestType == null) return NotFound();

            // Only the original technician can resubmit
            if (testRequestTestType.TechnicianId != technicianId)
            {
                TempData["Error"] = "Only the original technician can resubmit this test.";
                return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId });
            }

            // Can only resubmit if status is ToBeReviewed
            if (testRequestTestType.RequestStatus != RequestStatus.ToBeReviewed)
            {
                TempData["Error"] = "Only tests awaiting review can be resubmitted.";
                return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId });
            }

            // Update the result value if provided
            if (!string.IsNullOrWhiteSpace(adjustedResultValue))
            {
                // Find existing TestResult and update
                var testResult = await _context.TestResults
                    .FirstOrDefaultAsync(tr => tr.TestRequestId == testRequestId && tr.TestTypeId == testTypeId);
                if (testResult != null)
                {
                    testResult.ResultValue = adjustedResultValue;
                    // Re-evaluate abnormality
                    if (testRequestTestType.TestType.NormalRangeMin.HasValue && testRequestTestType.TestType.NormalRangeMax.HasValue)
                    {
                        if (decimal.TryParse(adjustedResultValue, out decimal resultDecimal))
                        {
                            testResult.IsAbnormal = resultDecimal < testRequestTestType.TestType.NormalRangeMin.Value ||
                                                   resultDecimal > testRequestTestType.TestType.NormalRangeMax.Value;
                        }
                    }
                    testResult.Notes = resubmitNotes ?? testResult.Notes;
                }
            }

            testRequestTestType.RequestStatus = RequestStatus.Completed;
            testRequestTestType.CompletionDateTime = DateTime.Now;
            testRequestTestType.ReviewNotes = resubmitNotes;
            // Clear previous verification info so a different technician can verify
            testRequestTestType.VerifiedById = null;
            testRequestTestType.VerifiedDateTime = null;
            testRequestTestType.VerificationNotes = null;

            // Log review history
            _context.TestReviewHistories.Add(new TestReviewHistory
            {
                TestRequestId = testRequestId,
                TestTypeId = testTypeId,
                ReviewerId = technicianId,
                Action = "Resubmitted",
                Notes = resubmitNotes
            });

            await _context.SaveChangesAsync();

            TempData["Message"] = $"Test resubmitted for verification: {testRequestTestType.TestType?.TestName}";
            return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId });
        }


        [HttpGet]
        public async Task<IActionResult> CaptureResult(int testRequestId, int testTypeId)
        {
            int technicianId = GetCurrentTechnicianId();

            var testRequestTestType = await _context.TestRequestTestTypes
                .Include(trt => trt.TestType)
                .Include(trt => trt.TestRequest)
                    .ThenInclude(tr => tr.Patient)
                        .ThenInclude(p => p.PatientConditions).ThenInclude(pc => pc.MedicalCondition)
                .Include(trt => trt.TestRequest)
                    .ThenInclude(tr => tr.Patient)
                        .ThenInclude(p => p.PatientAllergies).ThenInclude(pa => pa.Allergy)
                .Include(trt => trt.TestRequest)
                    .ThenInclude(tr => tr.Patient)
                        .ThenInclude(p => p.PatientMedications).ThenInclude(pm => pm.Medication)
                .FirstOrDefaultAsync(trt => trt.TestRequestId == testRequestId && trt.TestTypeId == testTypeId);

            if (testRequestTestType == null) return NotFound();

            // Only the assigned technician can capture results when test is InProgress
            if (testRequestTestType.TechnicianId != technicianId || testRequestTestType.RequestStatus != RequestStatus.InProgress)
            {
                TempData["Error"] = "You cannot capture results for this test at this time.";
                return RedirectToAction(nameof(ProcessTestTypes), new { requestId = testRequestId });
            }

            var patient = testRequestTestType.TestRequest.Patient;
            var request = testRequestTestType.TestRequest;

            var model = new CaptureResultViewModel
            {
                TestRequestId = testRequestId,
                TestTypeId = testTypeId,
                TestName = testRequestTestType.TestType.TestName,
                PatientName = patient.FirstName + " " + patient.LastName,
                UnitsOfMeasurement = testRequestTestType.TestType.UnitsOfMeasurement ?? "",
                NormalRangeMin = testRequestTestType.TestType.NormalRangeMin,
                NormalRangeMax = testRequestTestType.TestType.NormalRangeMax,
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

            var testRequestTestType = await _context.TestRequestTestTypes
                .Include(trt => trt.TestType)
                .Include(trt => trt.TestRequest).ThenInclude(tr => tr.TestRequestTestTypes)
                .FirstOrDefaultAsync(trt => trt.TestRequestId == model.TestRequestId && trt.TestTypeId == model.TestTypeId);

            if (testRequestTestType == null) return NotFound();

            if (testRequestTestType.TechnicianId != technicianId || testRequestTestType.RequestStatus != RequestStatus.InProgress)
            {
                TempData["Error"] = "You cannot capture results for this test at this time.";
                return RedirectToAction(nameof(ProcessTestTypes), new { requestId = model.TestRequestId });
            }

            if (!ModelState.IsValid)
                return View(model);

            // Determine abnormality
            bool isAbnormal = false;
            if (testRequestTestType.TestType.NormalRangeMin.HasValue && testRequestTestType.TestType.NormalRangeMax.HasValue)
            {
                if (decimal.TryParse(model.ResultValue, out decimal resultDecimal))
                {
                    isAbnormal = resultDecimal < testRequestTestType.TestType.NormalRangeMin.Value ||
                                 resultDecimal > testRequestTestType.TestType.NormalRangeMax.Value;
                }
            }

            // Create TestResult
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

            // Update test status
            testRequestTestType.CompletionDateTime = DateTime.Now;
            testRequestTestType.RequestStatus = RequestStatus.Completed;

            // Check if request is fully completed
            var request = testRequestTestType.TestRequest;
            bool allCompleted = request.TestRequestTestTypes.All(trt => trt.RequestStatus == RequestStatus.Completed || trt.RequestStatus == RequestStatus.Verified);
            if (allCompleted)
            {
                request.RequestStatus = RequestStatus.Completed;
            }

            await _context.SaveChangesAsync();

            TempData["Message"] = $"Results captured for test: {testRequestTestType.TestType.TestName}";
            return RedirectToAction(nameof(ProcessTestTypes), new { requestId = model.TestRequestId });
        }





        #region Reports

        [HttpGet]
        public IActionResult CompletedTestsReport()
        {
            var model = new TechnicianReportFilterViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompletedTestsReport(TechnicianReportFilterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            int technicianId = GetCurrentTechnicianId();

            var pdfBytes = await _pdfService.GenerateTechnicianCompletedTestsReport(technicianId, model.StartDate, model.EndDate);

            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                TempData["Error"] = "PDF generation is not yet implemented or no data found.";
                return RedirectToAction(nameof(CompletedTestsReport));
            }

            string fileName = $"CompletedTests_{model.StartDate:yyyyMMdd}-{model.EndDate:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        #endregion



        private async Task NotifyDoctorAllTestsVerified(TestRequest request)
        {
            var doctor = request.Doctor;
            if (doctor == null || string.IsNullOrEmpty(doctor.Email)) return;

            // Generate PDF of all results
            byte[] pdfBytes = await _pdfService.GenerateTestResultsPdf(request.Id);

            string subject = $"All Tests Verified – Request #{request.Id}";
            string body = $"Dear Dr. {doctor.LastName},\n\n" +
                          $"All tests for request #{request.Id} (Patient: {request.Patient?.FirstName} {request.Patient?.LastName}) " +
                          $"have been verified and are ready for your review.\n\n" +
                          $"Please log in to the system to view and release the results.";

            // In production, attach the PDF using MailKit. Here we'll use a simplified email.
            await _emailService.SendEmailAsync(doctor.Email, subject, body);
            // To attach PDF, you would need to implement attachment support in IEmailService.
        }




      


        #region Helpers

        private int GetCurrentTechnicianId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out int id) ? id : 0;
        }

        #endregion
    }
}