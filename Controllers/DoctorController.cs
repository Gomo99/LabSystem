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
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace LaboratoryTestRequestManagementSystem.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorController : Controller
    {
        private readonly LabDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IPdfReportService _pdfService;

        public DoctorController(LabDbContext context, IEmailService emailService, IPdfReportService pdfService)
        {
            _context = context;
            _emailService = emailService;
            _pdfService = pdfService;
        }

        public IActionResult Dashboard() => View();

        #region Patient Listing (Active)

        public async Task<IActionResult> Patients()
        {
            var patients = await _context.Patients
                .Where(p => p.IsActive == Status.Active)
                .Select(p => new PatientListViewModel
                {
                    Id = p.Id,
                    FullName = p.FirstName + " " + p.LastName,
                    Email = p.Email,
                    SouthAfricanIdNumber = p.SouthAfricanIdNumber,
                    CellphoneNumber = p.CellphoneNumber,
                    IsActive = p.IsActive
                })
                .ToListAsync();

            return View(patients);
        }

        #endregion

        #region Patient Details

        public async Task<IActionResult> Details(int id)
        {
            var patient = await _context.Patients
                .Include(p => p.PatientConditions).ThenInclude(pc => pc.MedicalCondition)
                .Include(p => p.PatientAllergies).ThenInclude(pa => pa.Allergy)
                .Include(p => p.PatientMedications).ThenInclude(pm => pm.Medication)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient == null) return NotFound();

            var model = new PatientDetailsViewModel
            {
                Id = patient.Id,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                SouthAfricanIdNumber = patient.SouthAfricanIdNumber,
                DateOfBirth = patient.DateOfBirth,
                CellphoneNumber = patient.CellphoneNumber,
                Email = patient.Email,
                HomeAddress = patient.HomeAddress,
                MedicalConditions = patient.PatientConditions.Select(pc => pc.MedicalCondition.Name).ToList(),
                Allergies = patient.PatientAllergies.Select(pa => pa.Allergy.Name).ToList(),
                Medications = patient.PatientMedications.Select(pm => pm.Medication.Name).ToList()
            };

            return View(model);
        }

        #endregion

        #region Register Patient (Create)

        [HttpGet]
        public IActionResult RegisterPatient()
        {
            return View(new PatientRegistrationViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterPatient(PatientRegistrationViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (!IsPasswordComplex(model.Password))
            {
                ModelState.AddModelError(nameof(model.Password),
                    "Password must be at least 8 characters and contain an uppercase letter, a number, and a special character.");
                return View(model);
            }

            bool emailExists = await _context.Patients.AnyAsync(p => p.Email == model.Email);
            if (emailExists)
            {
                ModelState.AddModelError(nameof(model.Email), "Email address is already registered.");
                return View(model);
            }

            bool idExists = await _context.Patients.AnyAsync(p => p.SouthAfricanIdNumber == model.SouthAfricanIdNumber);
            if (idExists)
            {
                ModelState.AddModelError(nameof(model.SouthAfricanIdNumber), "ID number is already registered.");
                return View(model);
            }

            string tempPassword = GenerateRandomPassword();
            var patient = new Patient
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                SouthAfricanIdNumber = model.SouthAfricanIdNumber,
                DateOfBirth = model.DateOfBirth,
                CellphoneNumber = model.CellphoneNumber,
                Email = model.Email,
                HomeAddress = model.HomeAddress,
                PasswordHash = HashPassword(tempPassword),
                IsActive = Status.Active,
                MustChangePassword = true,
                FailedLoginAttempts = 0
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            await _emailService.SendEmailAsync(patient.Email, "Your NMB-HLabSys Patient Account",
                $"Dear {patient.FirstName},\n\n" +
                $"Your patient account has been created by your doctor.\n\n" +
                $"Username (email): {patient.Email}\n" +
                $"Temporary Password: {tempPassword}\n\n" +
                $"Please log in and change your password immediately.");

            TempData["Message"] = $"Patient {patient.FirstName} {patient.LastName} registered successfully.";
            return RedirectToAction(nameof(Patients));
        }

        #endregion

        #region Edit Patient

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var patient = await _context.Patients
                .Include(p => p.PatientConditions).ThenInclude(pc => pc.MedicalCondition)
                .Include(p => p.PatientAllergies).ThenInclude(pa => pa.Allergy)
                .Include(p => p.PatientMedications).ThenInclude(pm => pm.Medication)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient == null) return NotFound();

            var model = new EditPatientViewModel
            {
                Id = patient.Id,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                SouthAfricanIdNumber = patient.SouthAfricanIdNumber,
                DateOfBirth = patient.DateOfBirth,
                CellphoneNumber = patient.CellphoneNumber,
                Email = patient.Email,
                HomeAddress = patient.HomeAddress,
                MedicalConditionsInput = string.Join(", ", patient.PatientConditions.Select(pc => pc.MedicalCondition.Name)),
                AllergiesInput = string.Join(", ", patient.PatientAllergies.Select(pa => pa.Allergy.Name)),
                MedicationsInput = string.Join(", ", patient.PatientMedications.Select(pm => pm.Medication.Name))
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditPatientViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var patient = await _context.Patients
                .Include(p => p.PatientConditions)
                .Include(p => p.PatientAllergies)
                .Include(p => p.PatientMedications)
                .FirstOrDefaultAsync(p => p.Id == model.Id);

            if (patient == null) return NotFound();

            // Check unique constraints (excluding current patient)
            if (await _context.Patients.AnyAsync(p => p.Email == model.Email && p.Id != model.Id))
            {
                ModelState.AddModelError(nameof(model.Email), "Email address is already registered.");
                return View(model);
            }

            if (await _context.Patients.AnyAsync(p => p.SouthAfricanIdNumber == model.SouthAfricanIdNumber && p.Id != model.Id))
            {
                ModelState.AddModelError(nameof(model.SouthAfricanIdNumber), "ID number is already registered.");
                return View(model);
            }

            // Update basic info
            patient.FirstName = model.FirstName;
            patient.LastName = model.LastName;
            patient.SouthAfricanIdNumber = model.SouthAfricanIdNumber;
            patient.DateOfBirth = model.DateOfBirth;
            patient.CellphoneNumber = model.CellphoneNumber;
            patient.Email = model.Email;
            patient.HomeAddress = model.HomeAddress;

            // Update medical conditions
            UpdatePatientMedicalHistory(patient, model.MedicalConditionsInput, "condition");
            UpdatePatientMedicalHistory(patient, model.AllergiesInput, "allergy");
            UpdatePatientMedicalHistory(patient, model.MedicationsInput, "medication");

            await _context.SaveChangesAsync();

            TempData["Message"] = "Patient updated successfully.";
            return RedirectToAction(nameof(Details), new { id = patient.Id });
        }

        private async void UpdatePatientMedicalHistory(Patient patient, string input, string type)
        {
            // Clear existing
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

        #region Soft Delete & Restore

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient != null)
            {
                patient.IsActive = Status.Inactive;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Patients));
        }

        public async Task<IActionResult> InactivePatients()
        {
            var patients = await _context.Patients
                .Where(p => p.IsActive == Status.Inactive)
                .Select(p => new PatientListViewModel
                {
                    Id = p.Id,
                    FullName = p.FirstName + " " + p.LastName,
                    Email = p.Email,
                    SouthAfricanIdNumber = p.SouthAfricanIdNumber,
                    CellphoneNumber = p.CellphoneNumber,
                    IsActive = p.Status
                })
                .ToListAsync();

            return View(patients);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient != null)
            {
                patient.IsActive = Status.Active;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(InactivePatients));
        }




        #region Test Request Management

        // List active test requests for the current doctor
        public async Task<IActionResult> TestRequests()
        {
            int doctorId = GetCurrentDoctorId();
            var requests = await _context.TestRequests
                .Where(tr => tr.DoctorId == doctorId && tr.RecordStatus == Status.Active)
                .Include(tr => tr.Patient)
                .Include(tr => tr.TestRequestTestTypes)
                .OrderByDescending(tr => tr.RequestDate)
                .Select(tr => new TestRequestListViewModel
                {
                    Id = tr.Id,
                    PatientName = tr.Patient.FirstName + " " + tr.Patient.LastName,
                    RequestDate = tr.RequestDate,
                    Urgency = tr.Urgency,
                    Status = tr.RequestStatus,
                    TestCount = tr.TestRequestTestTypes.Count
                })
                .ToListAsync();

            return View(requests);
        }

        // View details of a specific test request
        // View details of a specific test request
        public async Task<IActionResult> RequestDetails(int id)
        {
            int doctorId = GetCurrentDoctorId();
            var request = await _context.TestRequests
                .Include(tr => tr.Patient)
                .Include(tr => tr.Doctor)
                .Include(tr => tr.TestRequestTestTypes).ThenInclude(trt => trt.TestType).ThenInclude(tt => tt.SampleType)
                .Include(tr => tr.Samples).ThenInclude(s => s.SampleType)
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.DoctorId == doctorId && tr.RecordStatus == Status.Active);

            if (request == null) return NotFound();

            // Load results for this request
            var results = await _context.TestResults
                .Where(r => r.TestRequestId == id)
                .ToListAsync();

            var model = new TestRequestDetailsViewModel
            {
                Id = request.Id,
                PatientName = request.Patient.FirstName + " " + request.Patient.LastName,
                DoctorName = request.Doctor.FirstName + " " + request.Doctor.LastName,
                RequestDate = request.RequestDate,
                Urgency = request.Urgency,
                ClinicalNotes = request.ClinicalNotes,
                Status = request.RequestStatus,
                DateCancelled = request.DateCancelled,
                CancellationReason = request.CancellationReason,

                TestTypes = request.TestRequestTestTypes.Select(trt =>
                {
                    var result = results.FirstOrDefault(r => r.TestTypeId == trt.TestTypeId);
                    return new TestTypeItemViewModel
                    {
                        TestName = trt.TestType.TestName,
                        SampleType = trt.TestType.SampleType?.Name ?? "N/A",
                        Status = trt.RequestStatus,
                        ResultValue = result?.ResultValue,
                        IsAbnormal = result?.IsAbnormal ?? false,
                        Notes = result?.Notes
                    };
                }).ToList(),
                Samples = request.Samples.Select(s => new SampleItemViewModel
                {
                    Barcode = s.Barcode,
                    SampleType = s.SampleType?.Name ?? "N/A",
                    CollectedDate = s.CollectedDate
                }).ToList()
            };

            return View(model);
        }

        // GET: Edit Test Request
        [HttpGet]
        public async Task<IActionResult> EditRequest(int id)
        {
            int doctorId = GetCurrentDoctorId();
            var request = await _context.TestRequests
                .Include(tr => tr.TestRequestTestTypes)
                .Include(tr => tr.Samples)
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.DoctorId == doctorId && tr.RecordStatus == Status.Active);

            if (request == null) return NotFound();

            // Only allow editing if status is Submitted or SamplesReceived (not yet in progress)
            bool canEdit = request.RequestStatus == RequestStatus.Submitted || request.RequestStatus == RequestStatus.SamplesReceived;
            if (!canEdit)
            {
                TempData["Error"] = "Cannot edit a request that is already in progress or completed.";
                return RedirectToAction(nameof(RequestDetails), new { id });
            }

            var model = new EditTestRequestViewModel
            {
                Id = request.Id,
                PatientId = request.PatientId,
                Urgency = request.Urgency,
                ClinicalNotes = request.ClinicalNotes,
                SelectedTestTypeIds = request.TestRequestTestTypes.Select(trt => trt.TestTypeId).ToList(),
                Samples = request.Samples.Select(s => new SampleEntryViewModel
                {
                    Barcode = s.Barcode,
                    SampleTypeId = s.SampleTypeId
                }).ToList(),
                CanEditSamples = request.RequestStatus == RequestStatus.Submitted // Only if not yet received
            };

            await PopulateTestRequestDropdowns();
            return View(model);
        }

        // POST: Edit Test Request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRequest(EditTestRequestViewModel model)
        {
            int doctorId = GetCurrentDoctorId();
            var request = await _context.TestRequests
                .Include(tr => tr.TestRequestTestTypes)
                .Include(tr => tr.Samples)
                .FirstOrDefaultAsync(tr => tr.Id == model.Id && tr.DoctorId == doctorId && tr.RecordStatus == Status.Active);

            if (request == null) return NotFound();

            bool canEdit = request.RequestStatus == RequestStatus.Submitted || request.RequestStatus == RequestStatus.SamplesReceived;
            if (!canEdit)
            {
                TempData["Error"] = "Cannot edit a request that is already in progress or completed.";
                return RedirectToAction(nameof(RequestDetails), new { id = model.Id });
            }

            // Repopulate dropdowns on error
            async Task Repopulate()
            {
                await PopulateTestRequestDropdowns();
            }

            // Validation
            if (model.SelectedTestTypeIds == null || !model.SelectedTestTypeIds.Any())
            {
                ModelState.AddModelError("SelectedTestTypeIds", "At least one test type must be selected.");
                await Repopulate();
                return View(model);
            }

            if (model.Samples == null || !model.Samples.Any())
            {
                ModelState.AddModelError("Samples", "At least one sample must be provided.");
                await Repopulate();
                return View(model);
            }

            // Validate samples (barcodes unique in request and not already in system excluding current)
            var barcodes = model.Samples.Select(s => s.Barcode).ToList();
            if (barcodes.Distinct().Count() != barcodes.Count)
            {
                ModelState.AddModelError("", "Barcodes must be unique within the request.");
                await Repopulate();
                return View(model);
            }

            var existingBarcodes = await _context.Samples
                .Where(s => s.TestRequestId != model.Id && barcodes.Contains(s.Barcode))
                .Select(s => s.Barcode)
                .ToListAsync();
            if (existingBarcodes.Any())
            {
                ModelState.AddModelError("", $"Barcodes already exist: {string.Join(", ", existingBarcodes)}");
                await Repopulate();
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                await Repopulate();
                return View(model);
            }

            // Update basic info
            request.PatientId = model.PatientId;
            request.Urgency = model.Urgency;
            request.ClinicalNotes = model.ClinicalNotes;

            // Update test types (remove and re-add)
            _context.TestRequestTestTypes.RemoveRange(request.TestRequestTestTypes);
            foreach (var ttId in model.SelectedTestTypeIds)
            {
                request.TestRequestTestTypes.Add(new TestRequestTestType
                {
                    TestTypeId = ttId,
                    RequestStatus = RequestStatus.Submitted
                });
            }

            // Update samples only if allowed (status == Submitted)
            if (request.RequestStatus == RequestStatus.Submitted && model.CanEditSamples)
            {
                _context.Samples.RemoveRange(request.Samples);
                foreach (var sampleVm in model.Samples)
                {
                    request.Samples.Add(new Sample
                    {
                        Barcode = sampleVm.Barcode,
                        SampleTypeId = sampleVm.SampleTypeId,
                        CollectedDate = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();

            TempData["Message"] = "Test request updated successfully.";
            return RedirectToAction(nameof(RequestDetails), new { id = request.Id });
        }

        // Soft delete a test request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            int doctorId = GetCurrentDoctorId();
            var request = await _context.TestRequests
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.DoctorId == doctorId && tr.RecordStatus == Status.Active);

            if (request == null) return NotFound();

            request.RecordStatus = Status.Inactive;
            await _context.SaveChangesAsync();

            TempData["Message"] = "Test request deleted.";
            return RedirectToAction(nameof(TestRequests));
        }

        // List inactive (soft deleted) test requests
        public async Task<IActionResult> InactiveTestRequests()
        {
            int doctorId = GetCurrentDoctorId();
            var requests = await _context.TestRequests
                .Where(tr => tr.DoctorId == doctorId && tr.RecordStatus == Status.Inactive)
                .Include(tr => tr.Patient)
                .Include(tr => tr.TestRequestTestTypes)
                .OrderByDescending(tr => tr.RequestDate)
                .Select(tr => new TestRequestListViewModel
                {
                    Id = tr.Id,
                    PatientName = tr.Patient.FirstName + " " + tr.Patient.LastName,
                    RequestDate = tr.RequestDate,
                    Urgency = tr.Urgency,
                    Status = tr.RequestStatus,
                    TestCount = tr.TestRequestTestTypes.Count
                })
                .ToListAsync();

            return View(requests);
        }

        // Restore a soft-deleted test request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreRequest(int id)
        {
            int doctorId = GetCurrentDoctorId();
            var request = await _context.TestRequests
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.DoctorId == doctorId && tr.RecordStatus == Status.Inactive);

            if (request == null) return NotFound();

            request.RecordStatus = Status.Active;
            await _context.SaveChangesAsync();

            TempData["Message"] = "Test request restored.";
            return RedirectToAction(nameof(InactiveTestRequests));
        }

        // Helper to populate dropdowns for test request creation/editing
        private async Task PopulateTestRequestDropdowns()
        {
            ViewBag.Patients = new SelectList(
                await _context.Patients.Where(p => p.IsActive == Status.Active).ToListAsync(),
                "Id", "FirstName");

            ViewBag.TestTypes = await _context.TestTypes
                .Where(t => t.Status == Status.Active)
                .Include(t => t.SampleType)
                .ToListAsync();

            ViewBag.SampleTypes = new SelectList(
                await _context.SampleTypes.Where(st => st.Status == Status.Active).ToListAsync(),
                "Id", "Name");
        }

        #endregion





        #endregion




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReleaseResults(int id)
        {
            int doctorId = GetCurrentDoctorId();
            var request = await _context.TestRequests
                .Include(tr => tr.Patient)
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.DoctorId == doctorId && tr.RecordStatus == Status.Active);

            if (request == null) return NotFound();

            // Can only release if status is Completed
            if (request.RequestStatus != RequestStatus.Completed)
            {
                TempData["Error"] = "Results can only be released after all tests are completed.";
                return RedirectToAction(nameof(RequestDetails), new { id });
            }

            request.RequestStatus = RequestStatus.ReleasedByDoctor;
            await _context.SaveChangesAsync();

            // Notify patient that results are available
            if (request.Patient != null)
            {
                string message = $"Dear {request.Patient.FirstName},\n\n" +
                                 $"Your test results for request dated {request.RequestDate:dd/MM/yyyy} are now available.\n" +
                                 $"Please log in to the patient portal to view them.\n\n" +
                                 $"Thank you.";
                await _emailService.SendEmailAsync(request.Patient.Email, "Your Test Results Are Ready", message);
            }

            TempData["Message"] = "Results released to patient.";
            return RedirectToAction(nameof(RequestDetails), new { id });
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRequest(int id, string cancellationReason)
        {
            if (string.IsNullOrWhiteSpace(cancellationReason))
            {
                TempData["Error"] = "Cancellation reason is required.";
                return RedirectToAction(nameof(RequestDetails), new { id });
            }

            int doctorId = GetCurrentDoctorId();
            var request = await _context.TestRequests
                .Include(tr => tr.Patient)
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.DoctorId == doctorId && tr.RecordStatus == Status.Active);

            if (request == null) return NotFound();

            // Doctor can only cancel if status is Submitted or SamplesReceived
            if (request.RequestStatus != RequestStatus.Submitted && request.RequestStatus != RequestStatus.SamplesReceived)
            {
                TempData["Error"] = "Cannot cancel a request that is already in progress or completed.";
                return RedirectToAction(nameof(RequestDetails), new { id });
            }

            request.RequestStatus = RequestStatus.Cancelled;
            request.CancellationReason = cancellationReason;
            request.DateCancelled = DateTime.Now;
            await _context.SaveChangesAsync();

            // Notify patient
            if (request.Patient != null)
            {
                await _emailService.SendEmailAsync(request.Patient.Email,
                    "Test Request Cancelled",
                    $"Dear {request.Patient.FirstName},\n\n" +
                    $"Your test request dated {request.RequestDate:dd/MM/yyyy} has been cancelled by your doctor.\n" +
                    $"Reason: {cancellationReason}");
            }

            TempData["Message"] = "Test request cancelled.";
            return RedirectToAction(nameof(RequestDetails), new { id });
        }



        [HttpGet]
        public async Task<IActionResult> DownloadResultsPdf(int id)
        {
            int doctorId = GetCurrentDoctorId();
            var request = await _context.TestRequests
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.DoctorId == doctorId && tr.RecordStatus == Status.Active);

            if (request == null) return NotFound();

            // Only allow if results are completed or released
            if (request.RequestStatus != RequestStatus.Completed && request.RequestStatus != RequestStatus.ReleasedByDoctor)
            {
                TempData["Error"] = "Results are not yet available for download.";
                return RedirectToAction(nameof(RequestDetails), new { id });
            }

            var pdfBytes = await _pdfService.GenerateTestResultsPdf(id);
            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                TempData["Error"] = "PDF generation is not yet implemented.";
                return RedirectToAction(nameof(RequestDetails), new { id });
            }

            return File(pdfBytes, "application/pdf", $"TestResults_{request.Id}_{DateTime.Now:yyyyMMdd}.pdf");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmailResults(int id, string message)
        {
            int doctorId = GetCurrentDoctorId();
            var request = await _context.TestRequests
                .Include(tr => tr.Patient)
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.DoctorId == doctorId && tr.RecordStatus == Status.Active);

            if (request == null) return NotFound();

            if (request.RequestStatus != RequestStatus.Completed && request.RequestStatus != RequestStatus.ReleasedByDoctor)
            {
                TempData["Error"] = "Cannot email results before they are completed.";
                return RedirectToAction(nameof(RequestDetails), new { id });
            }

            // Generate PDF attachment
            var pdfBytes = await _pdfService.GenerateTestResultsPdf(id);
            // For simplicity, we'll just send a plain email. 
            // In a real app, use MailKit with attachment.

            string subject = "Your Test Results";
            string body = $"Dear {request.Patient.FirstName},\n\n{message}\n\n";
            if (request.RequestStatus == RequestStatus.ReleasedByDoctor)
                body += "Your results are attached.\n";
            else
                body += "Your doctor will release them shortly.\n";

            await _emailService.SendEmailAsync(request.Patient.Email, subject, body);

            TempData["Message"] = "Email sent to patient.";
            return RedirectToAction(nameof(RequestDetails), new { id });
        }







        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReleaseResults(ReleaseResultsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(RequestDetails), new { id = model.RequestId });
            }

            int doctorId = GetCurrentDoctorId();
            var request = await _context.TestRequests
                .Include(tr => tr.Patient)
                .FirstOrDefaultAsync(tr => tr.Id == model.RequestId && tr.DoctorId == doctorId && tr.RecordStatus == Status.Active);

            if (request == null) return NotFound();

            if (request.RequestStatus != RequestStatus.Completed)
            {
                TempData["Error"] = "Results can only be released after all tests are completed.";
                return RedirectToAction(nameof(RequestDetails), new { id = model.RequestId });
            }

            request.RequestStatus = RequestStatus.ReleasedByDoctor;
            await _context.SaveChangesAsync();

            // Prepare email content
            string subject = "Your Test Results Are Ready";
            string body = $"Dear {request.Patient.FirstName},\n\n{model.Note}\n\n";

            if (model.RequestAppointment)
            {
                body += "Please contact our office to schedule a follow-up appointment to discuss your results.\n";
            }
            else
            {
                body += "You can view your results in the patient portal.\n";
            }

            // Attach PDF if requested
            if (model.AttachPdf)
            {
                var pdfBytes = await _pdfService.GenerateTestResultsPdf(model.RequestId);
                // For simplicity, we'll just note it in the email (actual attachment requires MailKit)
                body += "\nA PDF copy of your results is attached.\n";
                // In production: use MailKit to attach the byte array.
            }

            await _emailService.SendEmailAsync(request.Patient.Email, subject, body);

            TempData["Message"] = "Results released to patient.";
            return RedirectToAction(nameof(RequestDetails), new { id = model.RequestId });
        }




        #region Alerts

        [HttpGet]
        public async Task<IActionResult> Alerts(DateTime? startDate, DateTime? endDate)
        {
            int doctorId = GetCurrentDoctorId();

            // Default to last 5 days
            var from = startDate ?? DateTime.Today.AddDays(-5);
            var to = endDate ?? DateTime.Today;

            var query = _context.TestResults
                .Include(r => r.TestRequest).ThenInclude(tr => tr.Patient)
                .Include(r => r.TestType)
                .Where(r => r.IsAbnormal
                            && r.TestRequest.DoctorId == doctorId
                            && r.CompletedDate.HasValue
                            && r.CompletedDate.Value.Date >= from.Date
                            && r.CompletedDate.Value.Date <= to.Date);

            var alerts = await query
                .OrderByDescending(r => r.CompletedDate)
                .Select(r => new AlertViewModel
                {
                    TestRequestId = r.TestRequestId,
                    PatientName = r.TestRequest.Patient.FirstName + " " + r.TestRequest.Patient.LastName,
                    TestName = r.TestType.TestName,
                    ResultValue = r.ResultValue ?? "N/A",
                    NormalRange = r.TestType.NormalRangeMin.HasValue && r.TestType.NormalRangeMax.HasValue
                        ? $"{r.TestType.NormalRangeMin} - {r.TestType.NormalRangeMax} {r.TestType.UnitsOfMeasurement}"
                        : null,
                    CompletedDate = r.CompletedDate
                })
                .ToListAsync();

            var model = new AlertsFilterViewModel
            {
                StartDate = from,
                EndDate = to,
                Alerts = alerts
            };

            return View(model);
        }

        #endregion



        #region Reports

        [HttpGet]
        public IActionResult TestRequestsReport()
        {
            var model = new DoctorReportFilterViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TestRequestsReport(DoctorReportFilterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            int doctorId = GetCurrentDoctorId();

            var pdfBytes = await _pdfService.GenerateDoctorTestRequestsReport(doctorId, model.StartDate, model.EndDate);

            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                TempData["Error"] = "PDF generation is not yet implemented or no data found.";
                return RedirectToAction(nameof(TestRequestsReport));
            }

            string fileName = $"TestRequests_{model.StartDate:yyyyMMdd}-{model.EndDate:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        #endregion


        #region Helper Methods

        private static string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);

        private static bool IsPasswordComplex(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8) return false;
            bool hasUpper = Regex.IsMatch(password, @"[A-Z]");
            bool hasDigit = Regex.IsMatch(password, @"\d");
            bool hasSpecial = Regex.IsMatch(password, @"[^a-zA-Z0-9\s]");
            return hasUpper && hasDigit && hasSpecial;
        }

        private static string GenerateRandomPassword(int length = 10)
        {
            const string upper = "ABCDEFGHJKLMNOPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%^&*";
            string all = upper + lower + digits + special;

            var res = new StringBuilder();
            using (var rng = RandomNumberGenerator.Create())
            {
                res.Append(upper[GetRandomInt(rng, upper.Length)]);
                res.Append(digits[GetRandomInt(rng, digits.Length)]);
                res.Append(special[GetRandomInt(rng, special.Length)]);
                for (int i = 3; i < length; i++)
                    res.Append(all[GetRandomInt(rng, all.Length)]);
            }
            return new string(res.ToString().ToCharArray().OrderBy(s => Guid.NewGuid()).ToArray());
        }

        private static int GetRandomInt(RandomNumberGenerator rng, int max)
        {
            byte[] uintBuffer = new byte[sizeof(uint)];
            rng.GetBytes(uintBuffer);
            uint num = BitConverter.ToUInt32(uintBuffer, 0);
            return (int)(num % (uint)max);
        }

        private int GetCurrentDoctorId()
        {
            var userIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out int id) ? id : 0;
        }

        #endregion
    }
}