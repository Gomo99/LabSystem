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
using System.Text.Json;

namespace LaboratoryTestRequestManagementSystem.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientController : Controller
    {
        private readonly LabDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IPdfReportService _pdfService;

        // Standardized TempData keys
        private const string SuccessMessageKey = "SuccessMessage";
        private const string ErrorMessageKey = "ErrorMessage";

        private readonly INotificationService _notificationService;  // NEW

        public PatientController(LabDbContext context, IEmailService emailService,
                                 IPdfReportService pdfService, INotificationService notificationService) // NEW param
        {
            _context = context;
            _emailService = emailService;
            _pdfService = pdfService;
            _notificationService = notificationService;  // NEW
        }

        // ======================================================================
        //  HELPER METHODS (CLEAN + REUSABLE)
        // ======================================================================
        private void SetSuccess(string message)
        {
            TempData[SuccessMessageKey] = message;
        }

        private void SetError(string message)
        {
            TempData[ErrorMessageKey] = message;
        }

        public IActionResult DashBoard() => View();

        private int GetCurrentPatientId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out int id) ? id : 0;
        }

        #region Profile Management

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            int patientId = GetCurrentPatientId();
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null) return NotFound();

            var model = new PatientProfileViewModel
            {
                Id = patient.Id,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                SouthAfricanIdNumber = patient.SouthAfricanIdNumber,
                DateOfBirth = patient.DateOfBirth,
                CellphoneNumber = patient.CellphoneNumber,
                Email = patient.Email,
                HomeAddress = patient.HomeAddress
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(PatientProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            int patientId = GetCurrentPatientId();
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null) return NotFound();

            // Check unique constraints (excluding current patient)
            if (await _context.Patients.AnyAsync(p => p.Email == model.Email && p.Id != patientId))
            {
                ModelState.AddModelError(nameof(model.Email), "Email address is already registered.");
                return View(model);
            }

            if (await _context.Patients.AnyAsync(p => p.SouthAfricanIdNumber == model.SouthAfricanIdNumber && p.Id != patientId))
            {
                ModelState.AddModelError(nameof(model.SouthAfricanIdNumber), "ID number is already registered.");
                return View(model);
            }

            patient.FirstName = model.FirstName;
            patient.LastName = model.LastName;
            patient.SouthAfricanIdNumber = model.SouthAfricanIdNumber;
            patient.DateOfBirth = model.DateOfBirth;
            patient.CellphoneNumber = model.CellphoneNumber;
            patient.Email = model.Email;
            patient.HomeAddress = model.HomeAddress;

            await _context.SaveChangesAsync();

            SetSuccess("Profile updated successfully.");
            return RedirectToAction(nameof(Profile));
        }

        #endregion

        #region Medical History

        [HttpGet]
        public async Task<IActionResult> MedicalHistory()
        {
            int patientId = GetCurrentPatientId();
            var patient = await _context.Patients
                .Include(p => p.PatientConditions).ThenInclude(pc => pc.MedicalCondition)
                .Include(p => p.PatientAllergies).ThenInclude(pa => pa.Allergy)
                .Include(p => p.PatientMedications).ThenInclude(pm => pm.Medication)
                .FirstOrDefaultAsync(p => p.Id == patientId);

            if (patient == null) return NotFound();

            var model = new PatientMedicalHistoryViewModel
            {
                PatientId = patientId,
                MedicalConditionsInput = string.Join(", ", patient.PatientConditions.Select(pc => pc.MedicalCondition.Name)),
                AllergiesInput = string.Join(", ", patient.PatientAllergies.Select(pa => pa.Allergy.Name)),
                MedicationsInput = string.Join(", ", patient.PatientMedications.Select(pm => pm.Medication.Name))
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MedicalHistory(PatientMedicalHistoryViewModel model)
        {
            int patientId = GetCurrentPatientId();
            var patient = await _context.Patients
                .Include(p => p.PatientConditions)
                .Include(p => p.PatientAllergies)
                .Include(p => p.PatientMedications)
                .FirstOrDefaultAsync(p => p.Id == patientId);

            if (patient == null) return NotFound();

            // Update medical conditions
            await UpdatePatientMedicalHistory(patient, model.MedicalConditionsInput, "condition");
            await UpdatePatientMedicalHistory(patient, model.AllergiesInput, "allergy");
            await UpdatePatientMedicalHistory(patient, model.MedicationsInput, "medication");

            await _context.SaveChangesAsync();

            SetSuccess("Medical history updated successfully.");
            return RedirectToAction(nameof(MedicalHistory));
        }

        private async Task UpdatePatientMedicalHistory(Patient patient, string input, string type)
        {
            if (type == "condition")
                _context.PatientConditions.RemoveRange(patient.PatientConditions);
            else if (type == "allergy")
                _context.PatientAllergies.RemoveRange(patient.PatientAllergies);
            else if (type == "medication")
                _context.PatientMedications.RemoveRange(patient.PatientMedications);

            if (string.IsNullOrWhiteSpace(input)) return;

            var items = input.Split(',', StringSplitOptions.RemoveEmptyEntries)
                             .Select(s => s.Trim())
                             .Where(s => !string.IsNullOrEmpty(s))
                             .Distinct();

            foreach (var item in items)
            {
                if (type == "condition")
                {
                    var condition = await _context.MedicalConditions.FirstOrDefaultAsync(mc => mc.Name == item);
                    if (condition == null)
                    {
                        condition = new MedicalCondition { Name = item, Status = Status.Active };
                        _context.MedicalConditions.Add(condition);
                        await _context.SaveChangesAsync();
                    }
                    patient.PatientConditions.Add(new PatientCondition { PatientId = patient.Id, MedicalConditionId = condition.Id });
                }
                else if (type == "allergy")
                {
                    var allergy = await _context.Allergies.FirstOrDefaultAsync(a => a.Name == item);
                    if (allergy == null)
                    {
                        allergy = new Allergy { Name = item, Status = Status.Active };
                        _context.Allergies.Add(allergy);
                        await _context.SaveChangesAsync();
                    }
                    patient.PatientAllergies.Add(new PatientAllergy { PatientId = patient.Id, AllergyId = allergy.Id });
                }
                else if (type == "medication")
                {
                    var medication = await _context.Medications.FirstOrDefaultAsync(m => m.Name == item);
                    if (medication == null)
                    {
                        medication = new Medication { Name = item, Status = Status.Active };
                        _context.Medications.Add(medication);
                        await _context.SaveChangesAsync();
                    }
                    patient.PatientMedications.Add(new PatientMedication { PatientId = patient.Id, MedicationId = medication.Id });
                }
            }
        }

        #endregion

        #region Test Requests & Results

        public async Task<IActionResult> TestRequests()
        {
            int patientId = GetCurrentPatientId();
            var requests = await _context.TestRequests
                .Where(tr => tr.PatientId == patientId && tr.RecordStatus == Status.Active)
                .Include(tr => tr.Doctor)
                .Include(tr => tr.TestRequestTestTypes)
                .OrderByDescending(tr => tr.RequestDate)
                .Select(tr => new PatientTestRequestListViewModel
                {
                    Id = tr.Id,
                    DoctorName = tr.Doctor.FirstName + " " + tr.Doctor.LastName,
                    RequestDate = tr.RequestDate,
                    Urgency = tr.Urgency,
                    Status = tr.RequestStatus,
                    TestCount = tr.TestRequestTestTypes.Count
                })
                .ToListAsync();

            return View(requests);
        }

        public async Task<IActionResult> RequestDetails(int id)
        {
            int patientId = GetCurrentPatientId();
            var request = await _context.TestRequests
                .Include(tr => tr.Doctor)
                .Include(tr => tr.TestRequestTestTypes).ThenInclude(trt => trt.TestType)
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.PatientId == patientId && tr.RecordStatus == Status.Active);

            if (request == null) return NotFound();

            // Only load results if the doctor has released them
            var canView = request.RequestStatus == RequestStatus.ReleasedByDoctor;

            List<PatientTestResultItemViewModel> testResults = new();
            if (canView)
            {
                var results = await _context.TestResults
                    .Where(r => r.TestRequestId == id)
                    .ToListAsync();

                testResults = request.TestRequestTestTypes.Select(trt =>
                {
                    var result = results.FirstOrDefault(r => r.TestTypeId == trt.TestTypeId);
                    return new PatientTestResultItemViewModel
                    {
                        TestName = trt.TestType.TestName,
                        ResultValue = result?.ResultValue,
                        Units = trt.TestType.UnitsOfMeasurement,
                        NormalRange = trt.TestType.NormalRangeMin.HasValue && trt.TestType.NormalRangeMax.HasValue
                            ? $"{trt.TestType.NormalRangeMin} - {trt.TestType.NormalRangeMax}"
                            : null,
                        IsAbnormal = result?.IsAbnormal ?? false,
                        Notes = result?.Notes,
                        CompletedDate = result?.CompletedDate
                    };
                }).ToList();
            }

            var model = new PatientTestRequestDetailsViewModel
            {
                Id = request.Id,
                DoctorName = request.Doctor.FirstName + " " + request.Doctor.LastName,
                RequestDate = request.RequestDate,
                Urgency = request.Urgency,
                ClinicalNotes = request.ClinicalNotes,
                Status = request.RequestStatus,
                CanViewResults = canView,
                TestResults = testResults
            };

            // Check if the patient has been granted access to download the PDF
            bool granted = await _context.ReportAccessRequests
                .AnyAsync(r => r.TestRequestId == id && r.PatientId == patientId
                              && r.Status == AccessRequestStatus.Granted);
            model.HasGrantedAccess = granted;

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadResultsPdf(int id)
        {
            int patientId = GetCurrentPatientId();
            var request = await _context.TestRequests
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.PatientId == patientId);

            if (request == null) return NotFound();

            if (request.RequestStatus != RequestStatus.ReleasedByDoctor)
            {
                SetError("Results are not yet released.");
                return RedirectToAction(nameof(RequestDetails), new { id });
            }

            // NEW: Check access request
            var access = await _context.ReportAccessRequests
                .FirstOrDefaultAsync(r => r.TestRequestId == id && r.PatientId == patientId
                                          && r.Status == AccessRequestStatus.Granted);
            if (access == null)
            {
                // Redirect to the request page (show button to request access)
                return RedirectToAction(nameof(RequestDetails), new { id });
            }

            // original code continues
            var pdfBytes = await _pdfService.GenerateTestResultsPdf(id);
            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                SetError("PDF generation is not yet available.");
                return RedirectToAction(nameof(RequestDetails), new { id });
            }

            return File(pdfBytes, "application/pdf", $"Results_{id}_{DateTime.Now:yyyyMMdd}.pdf");
        }

        #endregion

        #region Result Tracking (Graph)

        [HttpGet]
        public async Task<IActionResult> TrackResults(int? testTypeId)
        {
            int patientId = GetCurrentPatientId();

            // Get all test types this patient has ever had results for
            var availableTestTypes = await _context.TestResults
                .Where(r => r.TestRequest.PatientId == patientId)
                .Select(r => r.TestType)
                .Distinct()
                .OrderBy(t => t.TestName)
                .ToListAsync();

            var model = new PatientTrackingViewModel
            {
                TestTypeOptions = new SelectList(availableTestTypes, "Id", "TestName", testTypeId)
            };

            if (testTypeId.HasValue)
            {
                var testType = await _context.TestTypes.FindAsync(testTypeId.Value);
                if (testType == null) return NotFound();

                var results = await _context.TestResults
                    .Where(r => r.TestTypeId == testTypeId && r.TestRequest.PatientId == patientId)
                    .Include(r => r.TestRequest)
                    .OrderBy(r => r.CompletedDate)
                    .Select(r => new TrackingDataPoint
                    {
                        Date = r.CompletedDate,
                        Value = r.ResultValue,
                        IsAbnormal = r.IsAbnormal
                    })
                    .ToListAsync();

                model.SelectedTestTypeId = testTypeId;
                model.TestName = testType.TestName;
                model.Units = testType.UnitsOfMeasurement;
                model.NormalMin = testType.NormalRangeMin;
                model.NormalMax = testType.NormalRangeMax;
                model.DataPoints = results;
            }

            return View(model);
        }

        #endregion

        #region Consent Management

        public async Task<IActionResult> DoctorAccess()
        {
            int patientId = GetCurrentPatientId();

            var accessGrants = await _context.DoctorPatientAccesses
                .Where(dpa => dpa.PatientId == patientId && dpa.Status == Status.Active)
                .Include(dpa => dpa.Doctor)
                .ToListAsync();

            var model = accessGrants.Select(dpa => new DoctorAccessViewModel
            {
                DoctorId = dpa.DoctorId,
                DoctorName = dpa.Doctor.FirstName + " " + dpa.Doctor.LastName,
                Email = dpa.Doctor.Email,
                GrantedDate = dpa.GrantedDate,
                HasAccess = true,
                SharedTestRequestIds = dpa.SharedTestRequestIds?.Split(',').Select(int.Parse).ToList() ?? new List<int>()
            }).ToList();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GrantAccess()
        {
            ViewBag.Doctors = new SelectList(
                await _context.Employees.Where(e => e.Role == UserRole.Doctor && e.IsActive == Status.Active).ToListAsync(),
                "Id", "FirstName");

            ViewBag.TestRequests = await _context.TestRequests
                .Where(tr => tr.PatientId == GetCurrentPatientId() && tr.RecordStatus == Status.Active)
                .OrderByDescending(tr => tr.RequestDate)
                .Select(tr => new { tr.Id, Label = $"#{tr.Id} - {tr.RequestDate:dd/MM/yyyy}" })
                .ToListAsync();

            return View(new GrantAccessViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GrantAccess(GrantAccessViewModel model)
        {
            int patientId = GetCurrentPatientId();

            var existing = await _context.DoctorPatientAccesses
                .FirstOrDefaultAsync(dpa => dpa.PatientId == patientId && dpa.DoctorId == model.DoctorId);

            if (existing != null)
            {
                existing.Status = Status.Active;
                existing.GrantedDate = DateTime.Now;
                existing.SharedTestRequestIds = string.Join(",", model.SelectedTestRequestIds);
            }
            else
            {
                _context.DoctorPatientAccesses.Add(new DoctorPatientAccess
                {
                    PatientId = patientId,
                    DoctorId = model.DoctorId,
                    GrantedDate = DateTime.Now,
                    SharedTestRequestIds = string.Join(",", model.SelectedTestRequestIds),
                    Status = Status.Active
                });
            }

            await _context.SaveChangesAsync();

            var doctor = await _context.Employees.FindAsync(model.DoctorId);
            if (doctor != null)
            {
                await _emailService.SendEmailAsync(doctor.Email, "Patient Access Granted",
                    $"Patient has granted you access to their test results.");
            }

            SetSuccess("Access granted successfully.");
            return RedirectToAction(nameof(DoctorAccess));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeAccess(int doctorId)
        {
            int patientId = GetCurrentPatientId();
            var access = await _context.DoctorPatientAccesses
                .FirstOrDefaultAsync(dpa => dpa.PatientId == patientId && dpa.DoctorId == doctorId);

            if (access != null)
            {
                access.Status = Status.Inactive;
                await _context.SaveChangesAsync();
            }

            SetSuccess("Access revoked.");
            return RedirectToAction(nameof(DoctorAccess));
        }

        #endregion

        #region Reports

        [HttpGet]
        public IActionResult ResultsReport()
        {
            return View(new PatientReportFilterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResultsReport(PatientReportFilterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            int patientId = GetCurrentPatientId();
            byte[] pdfBytes = await _pdfService.GeneratePatientResultsReport(patientId, model.StartDate, model.EndDate);

            if (pdfBytes.Length == 0)
            {
                SetError("No released results found in the selected date range, or PDF generation is not yet implemented.");
                return RedirectToAction(nameof(ResultsReport));
            }

            return File(pdfBytes, "application/pdf", $"MyResults_{model.StartDate:yyyyMMdd}-{model.EndDate:yyyyMMdd}.pdf");
        }

        #endregion


        #region Data Portability (Export / Import)

        [HttpGet]
        public async Task<IActionResult> ExportData()
        {
            int patientId = GetCurrentPatientId();

            var patient = await _context.Patients
                .Include(p => p.PatientConditions).ThenInclude(pc => pc.MedicalCondition)
                .Include(p => p.PatientAllergies).ThenInclude(pa => pa.Allergy)
                .Include(p => p.PatientMedications).ThenInclude(pm => pm.Medication)
                .FirstOrDefaultAsync(p => p.Id == patientId);

            if (patient == null) return NotFound();

            // Gather test results (from this lab)
            var results = await _context.TestResults
                .Where(r => r.TestRequest.PatientId == patientId
                            && r.TestRequest.RequestStatus == RequestStatus.ReleasedByDoctor)
                .Include(r => r.TestType)
                .Include(r => r.TestRequest)
                .ToListAsync();

            var importedResults = await _context.ImportedTestResults
                .Where(i => i.PatientId == patientId)
                .ToListAsync();

            var exportData = new
            {
                Patient = new
                {
                    FirstName = patient.FirstName,
                    LastName = patient.LastName,
                    SouthAfricanIdNumber = patient.SouthAfricanIdNumber,
                    DateOfBirth = patient.DateOfBirth,
                    CellphoneNumber = patient.CellphoneNumber,
                    Email = patient.Email,
                    HomeAddress = patient.HomeAddress
                },
                MedicalConditions = patient.PatientConditions
                    .Select(pc => pc.MedicalCondition.Name).ToList(),
                Allergies = patient.PatientAllergies
                    .Select(pa => pa.Allergy.Name).ToList(),
                Medications = patient.PatientMedications
                    .Select(pm => pm.Medication.Name).ToList(),
                TestResults = results.Select(r => new
                {
                    TestName = r.TestType.TestName,
                    ResultValue = r.ResultValue,
                    Units = r.TestType.UnitsOfMeasurement,
                    NormalRange = r.TestType.NormalRangeMin.HasValue && r.TestType.NormalRangeMax.HasValue
                        ? $"{r.TestType.NormalRangeMin} - {r.TestType.NormalRangeMax}"
                        : null,
                    Date = r.CompletedDate,
                    IsAbnormal = r.IsAbnormal,
                    Notes = r.Notes
                }),
                ImportedTestResults = importedResults.Select(i => new
                {
                    TestName = i.TestName,
                    ResultValue = i.ResultValue,
                    Units = i.Units,
                    NormalRange = i.NormalRange,
                    Date = i.ResultDate,
                    LabName = i.LabName
                })
            };

            string json = System.Text.Json.JsonSerializer.Serialize(exportData,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
            return File(bytes, "application/json", $"MyHealthData_{DateTime.Now:yyyyMMdd}.json");
        }

        [HttpGet]
        public async Task<IActionResult> ExportQrCode()
        {
            int patientId = GetCurrentPatientId();
            // Reuse the same data export
            var patient = await _context.Patients
                .Include(p => p.PatientConditions).ThenInclude(pc => pc.MedicalCondition)
                .Include(p => p.PatientAllergies).ThenInclude(pa => pa.Allergy)
                .Include(p => p.PatientMedications).ThenInclude(pm => pm.Medication)
                .FirstOrDefaultAsync(p => p.Id == patientId);

            if (patient == null) return NotFound();

            var results = await _context.TestResults
                .Where(r => r.TestRequest.PatientId == patientId
                            && r.TestRequest.RequestStatus == RequestStatus.ReleasedByDoctor)
                .Include(r => r.TestType)
                .Include(r => r.TestRequest)
                .ToListAsync();

            var importedResults = await _context.ImportedTestResults
                .Where(i => i.PatientId == patientId)
                .ToListAsync();

            var exportData = new
            {
                Patient = new
                {
                    patient.FirstName,
                    patient.LastName,
                    patient.SouthAfricanIdNumber,
                    patient.DateOfBirth,
                    patient.CellphoneNumber,
                    patient.Email,
                    patient.HomeAddress
                },
                MedicalConditions = patient.PatientConditions.Select(pc => pc.MedicalCondition.Name).ToList(),
                Allergies = patient.PatientAllergies.Select(pa => pa.Allergy.Name).ToList(),
                Medications = patient.PatientMedications.Select(pm => pm.Medication.Name).ToList(),
                TestResults = results.Select(r => new
                {
                    TestName = r.TestType.TestName,
                    r.ResultValue,
                    Units = r.TestType.UnitsOfMeasurement,
                    NormalRange = r.TestType.NormalRangeMin.HasValue && r.TestType.NormalRangeMax.HasValue
                        ? $"{r.TestType.NormalRangeMin} - {r.TestType.NormalRangeMax}"
                        : null,
                    Date = r.CompletedDate,
                    r.IsAbnormal,
                    r.Notes
                }),
                ImportedTestResults = importedResults.Select(i => new
                {
                    i.TestName,
                    i.ResultValue,
                    i.Units,
                    i.NormalRange,
                    Date = i.ResultDate,
                    i.LabName
                })
            };

            string json = System.Text.Json.JsonSerializer.Serialize(exportData);

            using var qrGenerator = new QRCoder.QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(json, QRCoder.QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
            byte[] qrBytes = qrCode.GetGraphic(20);

            return File(qrBytes, "image/png");
        }

        [HttpGet]
        public IActionResult ImportData()
        {
            return View(new ImportDataViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportData(ImportDataViewModel model)
        {
            if (model.File == null || model.File.Length == 0)
            {
                ModelState.AddModelError("File", "Please select a JSON file.");
                return View(model);
            }

            string json;
            using (var reader = new StreamReader(model.File.OpenReadStream()))
            {
                json = await reader.ReadToEndAsync();
            }

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(json);
            }
            catch
            {
                ModelState.AddModelError("File", "Invalid JSON file.");
                return View(model);
            }

            var root = doc.RootElement;

            // Update patient profile
            if (root.TryGetProperty("Patient", out JsonElement patientElement))
            {
                int patientId = GetCurrentPatientId();
                var patient = await _context.Patients.FindAsync(patientId);
                if (patient != null)
                {
                    patient.FirstName = patientElement.GetProperty("FirstName").GetString();
                    patient.LastName = patientElement.GetProperty("LastName").GetString();
                    patient.SouthAfricanIdNumber = patientElement.GetProperty("SouthAfricanIdNumber").GetString();
                    patient.DateOfBirth = patientElement.GetProperty("DateOfBirth").GetDateTime();
                    patient.CellphoneNumber = patientElement.GetProperty("CellphoneNumber").GetString();
                    patient.Email = patientElement.GetProperty("Email").GetString();
                    patient.HomeAddress = patientElement.GetProperty("HomeAddress").GetString();
                }
            }

            // Update medical history
            if (root.TryGetProperty("MedicalConditions", out JsonElement conditions))
            {
                var patientId = GetCurrentPatientId();
                var patient = await _context.Patients.Include(p => p.PatientConditions).FirstOrDefaultAsync(p => p.Id == patientId);
                if (patient != null)
                {
                    await UpdatePatientMedicalHistoryFromList(patient, conditions.EnumerateArray().Select(c => c.GetString()).ToList(), "condition");
                }
            }

            if (root.TryGetProperty("Allergies", out JsonElement allergies))
            {
                var patientId = GetCurrentPatientId();
                var patient = await _context.Patients.Include(p => p.PatientAllergies).FirstOrDefaultAsync(p => p.Id == patientId);
                if (patient != null)
                {
                    await UpdatePatientMedicalHistoryFromList(patient, allergies.EnumerateArray().Select(a => a.GetString()).ToList(), "allergy");
                }
            }

            if (root.TryGetProperty("Medications", out JsonElement medications))
            {
                var patientId = GetCurrentPatientId();
                var patient = await _context.Patients.Include(p => p.PatientMedications).FirstOrDefaultAsync(p => p.Id == patientId);
                if (patient != null)
                {
                    await UpdatePatientMedicalHistoryFromList(patient, medications.EnumerateArray().Select(m => m.GetString()).ToList(), "medication");
                }
            }

            // Import test results (both local and imported)
            var importedTestResults = new List<ImportedTestResult>();
            if (root.TryGetProperty("TestResults", out JsonElement testResults))
            {
                foreach (var item in testResults.EnumerateArray())
                {
                    importedTestResults.Add(new ImportedTestResult
                    {
                        PatientId = GetCurrentPatientId(),
                        TestName = item.GetProperty("TestName").GetString(),
                        ResultValue = item.GetProperty("ResultValue").GetString(),
                        Units = item.TryGetProperty("Units", out var u) ? u.GetString() : null,
                        NormalRange = item.TryGetProperty("NormalRange", out var nr) ? nr.GetString() : null,
                        ResultDate = item.TryGetProperty("Date", out var d) && d.ValueKind != JsonValueKind.Null ? d.GetDateTime() : null,
                        LabName = "Imported from external lab"
                    });
                }
            }

            if (root.TryGetProperty("ImportedTestResults", out JsonElement imported))
            {
                foreach (var item in imported.EnumerateArray())
                {
                    importedTestResults.Add(new ImportedTestResult
                    {
                        PatientId = GetCurrentPatientId(),
                        TestName = item.GetProperty("TestName").GetString(),
                        ResultValue = item.GetProperty("ResultValue").GetString(),
                        Units = item.TryGetProperty("Units", out var u) ? u.GetString() : null,
                        NormalRange = item.TryGetProperty("NormalRange", out var nr) ? nr.GetString() : null,
                        ResultDate = item.TryGetProperty("Date", out var d) && d.ValueKind != JsonValueKind.Null ? d.GetDateTime() : null,
                        LabName = item.TryGetProperty("LabName", out var ln) ? ln.GetString() : "External lab"
                    });
                }
            }

            if (importedTestResults.Any())
            {
                _context.ImportedTestResults.AddRange(importedTestResults);
            }

            await _context.SaveChangesAsync();

            SetSuccess("Data imported successfully.");
            return RedirectToAction(nameof(ImportData));
        }

        // Helper to update medical history from a list of names
        private async Task UpdatePatientMedicalHistoryFromList(Patient patient, List<string> items, string type)
        {
            if (type == "condition")
                _context.PatientConditions.RemoveRange(patient.PatientConditions);
            else if (type == "allergy")
                _context.PatientAllergies.RemoveRange(patient.PatientAllergies);
            else if (type == "medication")
                _context.PatientMedications.RemoveRange(patient.PatientMedications);

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item)) continue;

                if (type == "condition")
                {
                    var condition = await _context.MedicalConditions.FirstOrDefaultAsync(mc => mc.Name == item);
                    if (condition == null)
                    {
                        condition = new MedicalCondition { Name = item, Status = Status.Active };
                        _context.MedicalConditions.Add(condition);
                        await _context.SaveChangesAsync();
                    }
                    patient.PatientConditions.Add(new PatientCondition { PatientId = patient.Id, MedicalConditionId = condition.Id });
                }
                else if (type == "allergy")
                {
                    var allergy = await _context.Allergies.FirstOrDefaultAsync(a => a.Name == item);
                    if (allergy == null)
                    {
                        allergy = new Allergy { Name = item, Status = Status.Active };
                        _context.Allergies.Add(allergy);
                        await _context.SaveChangesAsync();
                    }
                    patient.PatientAllergies.Add(new PatientAllergy { PatientId = patient.Id, AllergyId = allergy.Id });
                }
                else if (type == "medication")
                {
                    var medication = await _context.Medications.FirstOrDefaultAsync(m => m.Name == item);
                    if (medication == null)
                    {
                        medication = new Medication { Name = item, Status = Status.Active };
                        _context.Medications.Add(medication);
                        await _context.SaveChangesAsync();
                    }
                    patient.PatientMedications.Add(new PatientMedication { PatientId = patient.Id, MedicationId = medication.Id });
                }
            }
        }

        #endregion


        #region Report Access Request

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestPdfAccess(int testRequestId)
        {
            int patientId = GetCurrentPatientId();

            var request = await _context.TestRequests
                .FirstOrDefaultAsync(tr => tr.Id == testRequestId && tr.PatientId == patientId && tr.RecordStatus == Status.Active);

            if (request == null) return NotFound();

            if (request.RequestStatus != RequestStatus.ReleasedByDoctor)
            {
                SetError("This request has not been released by the doctor yet.");
                return RedirectToAction(nameof(RequestDetails), new { id = testRequestId });
            }

            // Check for existing pending request
            var existing = await _context.ReportAccessRequests
                .FirstOrDefaultAsync(r => r.TestRequestId == testRequestId && r.PatientId == patientId
                                          && r.Status == AccessRequestStatus.Pending);
            if (existing != null)
            {
                SetError("A request is already pending. Please wait for the doctor's response.");
                return RedirectToAction(nameof(RequestDetails), new { id = testRequestId });
            }

            var accessRequest = new ReportAccessRequest
            {
                PatientId = patientId,
                DoctorId = request.DoctorId,
                TestRequestId = testRequestId,
                RequestDate = DateTime.Now,
                Status = AccessRequestStatus.Pending
            };
            _context.ReportAccessRequests.Add(accessRequest);
            await _context.SaveChangesAsync();

            // Notify the doctor
            string patientName = (await _context.Patients.FindAsync(patientId))?.FirstName ?? "A patient";
            string message = $"{patientName} has requested access to download the PDF for test request #{testRequestId}.";
            string link = $"/Doctor/PendingPdfRequests";   // or direct to the request if you prefer
            await _notificationService.CreateAsync(request.DoctorId, "Doctor", message, link);

            SetSuccess("Access request sent to your doctor. You can download the PDF once approved.");
            return RedirectToAction(nameof(RequestDetails), new { id = testRequestId });
        }

        #endregion





    }
}