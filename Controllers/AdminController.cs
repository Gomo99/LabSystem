using LaboratoryTestRequestManagementSystem.AppStatus;
using LaboratoryTestRequestManagementSystem.Data;
using LaboratoryTestRequestManagementSystem.Models;
using LaboratoryTestRequestManagementSystem.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaboratoryTestRequestManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly LabDbContext _context;

        // Standardized TempData keys
        private const string SuccessMessageKey = "SuccessMessage";
        private const string ErrorMessageKey = "ErrorMessage";

        public AdminController(LabDbContext context)
        {
            _context = context;
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

        // ======================================================================
        //  DASHBOARD
        // ======================================================================
        public IActionResult DashBoard() => View();

        #region Medical Conditions

        public async Task<IActionResult> MedicalConditions(AdminFilterViewModel filter)
        {
            var query = _context.MedicalConditions.AsQueryable();

            if (!filter.ShowInactive)
                query = query.Where(mc => mc.Status == Status.Active);

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                query = query.Where(mc => mc.Name.Contains(filter.SearchTerm));

            if (!string.IsNullOrWhiteSpace(filter.Category))
                query = query.Where(mc => mc.Category == filter.Category);

            var conditions = await query.OrderBy(mc => mc.Name).ToListAsync();

            ViewBag.Categories = await _context.MedicalConditions
                .Where(mc => mc.Category != null)
                .Select(mc => mc.Category)
                .Distinct()
                .ToListAsync();

            return View(conditions);
        }

        [HttpGet]
        public IActionResult CreateMedicalCondition() => View(new MedicalConditionViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMedicalCondition(MedicalConditionViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var condition = new MedicalCondition
            {
                Name = model.Name,
                Category = model.Category,
                Status = Status.Active
            };
            _context.MedicalConditions.Add(condition);
            await _context.SaveChangesAsync();

            SetSuccess("Medical condition created successfully.");
            return RedirectToAction("MedicalConditions");
        }

        [HttpGet]
        public async Task<IActionResult> EditMedicalCondition(int id)
        {
            var condition = await _context.MedicalConditions.FindAsync(id);
            if (condition == null) return NotFound();

            var model = new MedicalConditionViewModel
            {
                Id = condition.Id,
                Name = condition.Name,
                Category = condition.Category
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMedicalCondition(MedicalConditionViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var condition = await _context.MedicalConditions.FindAsync(model.Id);
            if (condition == null) return NotFound();

            condition.Name = model.Name;
            condition.Category = model.Category;
            await _context.SaveChangesAsync();

            SetSuccess("Medical condition updated successfully.");
            return RedirectToAction("MedicalConditions");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMedicalCondition(int id)
        {
            var condition = await _context.MedicalConditions.FindAsync(id);
            if (condition != null)
            {
                condition.Status = Status.Inactive;
                await _context.SaveChangesAsync();
                SetSuccess("Medical condition deactivated.");
            }
            else
            {
                SetError("Medical condition not found.");
            }
            return RedirectToAction("MedicalConditions");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreMedicalCondition(int id)
        {
            var condition = await _context.MedicalConditions.FindAsync(id);
            if (condition != null)
            {
                condition.Status = Status.Active;
                await _context.SaveChangesAsync();
                SetSuccess("Medical condition restored.");
            }
            else
            {
                SetError("Medical condition not found.");
            }
            return RedirectToAction("MedicalConditions", new AdminFilterViewModel { ShowInactive = true });
        }

        #endregion

        #region Allergies

        public async Task<IActionResult> Allergies(AdminFilterViewModel filter)
        {
            var query = _context.Allergies.AsQueryable();

            if (!filter.ShowInactive)
                query = query.Where(a => a.Status == Status.Active);

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                query = query.Where(a => a.Name.Contains(filter.SearchTerm));

            if (!string.IsNullOrWhiteSpace(filter.Category))
                query = query.Where(a => a.Category == filter.Category);

            var allergies = await query.OrderBy(a => a.Name).ToListAsync();

            ViewBag.Categories = await _context.Allergies
                .Where(a => a.Category != null)
                .Select(a => a.Category)
                .Distinct()
                .ToListAsync();

            return View(allergies);
        }

        [HttpGet]
        public IActionResult CreateAllergy() => View(new AllergyViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAllergy(AllergyViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var allergy = new Allergy
            {
                Name = model.Name,
                Category = model.Category,
                Status = Status.Active
            };
            _context.Allergies.Add(allergy);
            await _context.SaveChangesAsync();

            SetSuccess("Allergy created successfully.");
            return RedirectToAction("Allergies");
        }

        [HttpGet]
        public async Task<IActionResult> EditAllergy(int id)
        {
            var allergy = await _context.Allergies.FindAsync(id);
            if (allergy == null) return NotFound();

            var model = new AllergyViewModel
            {
                Id = allergy.Id,
                Name = allergy.Name,
                Category = allergy.Category
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAllergy(AllergyViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var allergy = await _context.Allergies.FindAsync(model.Id);
            if (allergy == null) return NotFound();

            allergy.Name = model.Name;
            allergy.Category = model.Category;
            await _context.SaveChangesAsync();

            SetSuccess("Allergy updated successfully.");
            return RedirectToAction("Allergies");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAllergy(int id)
        {
            var allergy = await _context.Allergies.FindAsync(id);
            if (allergy != null)
            {
                allergy.Status = Status.Inactive;
                await _context.SaveChangesAsync();
                SetSuccess("Allergy deactivated.");
            }
            else
            {
                SetError("Allergy not found.");
            }
            return RedirectToAction("Allergies");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreAllergy(int id)
        {
            var allergy = await _context.Allergies.FindAsync(id);
            if (allergy != null)
            {
                allergy.Status = Status.Active;
                await _context.SaveChangesAsync();
                SetSuccess("Allergy restored.");
            }
            else
            {
                SetError("Allergy not found.");
            }
            return RedirectToAction("Allergies", new AdminFilterViewModel { ShowInactive = true });
        }

        #endregion

        #region Medications

        public async Task<IActionResult> Medications(AdminFilterViewModel filter)
        {
            var query = _context.Medications.AsQueryable();

            if (!filter.ShowInactive)
                query = query.Where(m => m.Status == Status.Active);

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                query = query.Where(m => m.Name.Contains(filter.SearchTerm));

            if (!string.IsNullOrWhiteSpace(filter.Category))
                query = query.Where(m => m.Category == filter.Category);

            var medications = await query.OrderBy(m => m.Name).ToListAsync();

            ViewBag.Categories = await _context.Medications
                .Where(m => m.Category != null)
                .Select(m => m.Category)
                .Distinct()
                .ToListAsync();

            return View(medications);
        }

        [HttpGet]
        public IActionResult CreateMedication() => View(new MedicationViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMedication(MedicationViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var medication = new Medication
            {
                Name = model.Name,
                Category = model.Category,
                Status = Status.Active
            };
            _context.Medications.Add(medication);
            await _context.SaveChangesAsync();

            SetSuccess("Medication created successfully.");
            return RedirectToAction("Medications");
        }

        [HttpGet]
        public async Task<IActionResult> EditMedication(int id)
        {
            var medication = await _context.Medications.FindAsync(id);
            if (medication == null) return NotFound();

            var model = new MedicationViewModel
            {
                Id = medication.Id,
                Name = medication.Name,
                Category = medication.Category
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMedication(MedicationViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var medication = await _context.Medications.FindAsync(model.Id);
            if (medication == null) return NotFound();

            medication.Name = model.Name;
            medication.Category = model.Category;
            await _context.SaveChangesAsync();

            SetSuccess("Medication updated successfully.");
            return RedirectToAction("Medications");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMedication(int id)
        {
            var medication = await _context.Medications.FindAsync(id);
            if (medication != null)
            {
                medication.Status = Status.Inactive;
                await _context.SaveChangesAsync();
                SetSuccess("Medication deactivated.");
            }
            else
            {
                SetError("Medication not found.");
            }
            return RedirectToAction("Medications");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreMedication(int id)
        {
            var medication = await _context.Medications.FindAsync(id);
            if (medication != null)
            {
                medication.Status = Status.Active;
                await _context.SaveChangesAsync();
                SetSuccess("Medication restored.");
            }
            else
            {
                SetError("Medication not found.");
            }
            return RedirectToAction("Medications", new AdminFilterViewModel { ShowInactive = true });
        }

        #endregion
    }
}