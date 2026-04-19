using LaboratoryTestRequestManagementSystem.AppStatus;
using LaboratoryTestRequestManagementSystem.Data;
using LaboratoryTestRequestManagementSystem.Models;
using LaboratoryTestRequestManagementSystem.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LaboratoryTestRequestManagementSystem.Controllers
{
    [Authorize(Roles = "LabTechnician")]
    public class LabTechnicianController : Controller
    {
        private readonly LabDbContext _context;

        public LabTechnicianController(LabDbContext context)
        {
            _context = context;
        }

        public IActionResult Dashboard() => View();

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
                    CanComplete = trt.RequestStatus == RequestStatus.InProgress && trt.TechnicianId == technicianId
                }).ToList();

            var model = new AvailableTestTypeViewModel
            {
                TestRequestId = request.Id,
                PatientName = request.Patient.FirstName + " " + request.Patient.LastName,
                DoctorName = request.Doctor.FirstName + " " + request.Doctor.LastName,
                RequestDate = request.RequestDate,
                Urgency = request.Urgency,
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






        #region Helpers

        private int GetCurrentTechnicianId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out int id) ? id : 0;
        }

        #endregion
    }
}