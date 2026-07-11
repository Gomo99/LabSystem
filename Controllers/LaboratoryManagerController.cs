using LaboratoryTestRequestManagementSystem.AppStatus;
using LaboratoryTestRequestManagementSystem.Data;
using LaboratoryTestRequestManagementSystem.Models;
using LaboratoryTestRequestManagementSystem.Services;
using LaboratoryTestRequestManagementSystem.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace LaboratoryTestRequestManagementSystem.Controllers
{
    [Authorize(Roles = "LaboratoryManager")]
    public class LaboratoryManagerController : Controller
    {
        private readonly LabDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IPdfReportService _pdfService;
        private readonly INotificationService _notificationService;   // ← NEW

        // Standardized TempData keys
        private const string SuccessMessageKey = "SuccessMessage";
        private const string ErrorMessageKey = "ErrorMessage";

        // Updated constructor
        public LaboratoryManagerController(LabDbContext context, IEmailService emailService,
                                           IPdfReportService pdfService, INotificationService notificationService)
        {
            _context = context;
            _emailService = emailService;
            _pdfService = pdfService;
            _notificationService = notificationService;
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

        #region Test Categories (Soft Delete + Restore)

        public async Task<IActionResult> TestCategories()
        {
            var categories = await _context.TestCategories
                .Where(tc => tc.Status == Status.Active)
                .ToListAsync();
            return View(categories);
        }

        public async Task<IActionResult> InactiveTestCategories()
        {
            var categories = await _context.TestCategories
                .Where(tc => tc.Status == Status.Inactive)
                .ToListAsync();
            return View(categories);
        }

        [HttpGet]
        public IActionResult CreateTestCategory() => View(new TestCategoryViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTestCategory(TestCategoryViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // ✅ Uniqueness check
            bool nameExists = await _context.TestCategories
                .AnyAsync(tc => tc.CategoryName == model.CategoryName);
            if (nameExists)
            {
                ModelState.AddModelError(nameof(model.CategoryName), "A test category with this name already exists.");
                return View(model);
            }

            var category = new TestCategory
            {
                CategoryName = model.CategoryName,
                Description = model.Description,
                Status = Status.Active
            };
            _context.TestCategories.Add(category);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(TestCategories));
        }


        [HttpGet]
        public async Task<IActionResult> EditTestCategory(int id)
        {
            var category = await _context.TestCategories.FindAsync(id);
            if (category == null) return NotFound();

            var model = new TestCategoryViewModel
            {
                Id = category.Id,
                CategoryName = category.CategoryName,
                Description = category.Description
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTestCategory(TestCategoryViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var category = await _context.TestCategories.FindAsync(model.Id);
            if (category == null) return NotFound();

            // ✅ Uniqueness check (exclude self)
            bool nameExists = await _context.TestCategories
                .AnyAsync(tc => tc.CategoryName == model.CategoryName && tc.Id != model.Id);
            if (nameExists)
            {
                ModelState.AddModelError(nameof(model.CategoryName), "A test category with this name already exists.");
                return View(model);
            }

            category.CategoryName = model.CategoryName;
            category.Description = model.Description;
            category.Status = Status.Active;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(TestCategories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTestCategory(int id)
        {
            // Guard: don't let a manager silently orphan test types that reference this category
            bool inUse = await _context.TestTypes
                .AnyAsync(t => t.TestCategoryId == id && t.Status == Status.Active);
            if (inUse)
            {
                SetError("This test category is used by one or more active test types and cannot be deactivated.");
                return RedirectToAction(nameof(TestCategories));
            }

            var category = await _context.TestCategories.FindAsync(id);
            if (category != null)
            {
                category.Status = Status.Inactive;
                await _context.SaveChangesAsync();
                SetSuccess("Test category deactivated successfully.");
            }
            return RedirectToAction(nameof(TestCategories));
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreTestCategory(int id)
        {
            var category = await _context.TestCategories.FindAsync(id);
            if (category != null)
            {
                category.Status = Status.Active;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(InactiveTestCategories));
        }

        #endregion

        #region Test Types (Soft Delete + Restore)

        public async Task<IActionResult> TestTypes()
        {
            var testTypes = await _context.TestTypes
                .Where(t => t.Status == Status.Active)
                .Include(t => t.TestCategory)
                .Include(t => t.SampleType)
                .Include(t => t.TestTypeConsumables).ThenInclude(tc => tc.Consumable)
                .Include(t => t.TechnicianTestTypes).ThenInclude(ttt => ttt.Technician)   // ← NEW: shows assigned technicians
                .ToListAsync();
            return View(testTypes);
        }

        public async Task<IActionResult> InactiveTestTypes()
        {
            var testTypes = await _context.TestTypes
                .Where(t => t.Status == Status.Inactive)
                .Include(t => t.TestCategory)
                .Include(t => t.SampleType)
                .Include(t => t.TestTypeConsumables).ThenInclude(tc => tc.Consumable)
                .Include(t => t.TechnicianTestTypes).ThenInclude(ttt => ttt.Technician)   // ← NEW
                .ToListAsync();
            return View(testTypes);
        }

       

        [HttpGet]
        public async Task<IActionResult> CreateTestType()
        {
            await PopulateDropdowns(activeOnly: true);
            return View(new TestTypeViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTestType(TestTypeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(activeOnly: true);
                return View(model);
            }

            // ✅ Uniqueness check for test name
            bool nameExists = await _context.TestTypes
                .AnyAsync(t => t.TestName == model.TestName);
            if (nameExists)
            {
                ModelState.AddModelError(nameof(model.TestName), "A test type with this name already exists.");
                await PopulateDropdowns(activeOnly: true);
                return View(model);
            }

            var testType = new TestType
            {
                TestName = model.TestName,
                TestCategoryId = model.TestCategoryId,
                SampleTypeId = model.SampleTypeId,
                UnitsOfMeasurement = model.UnitsOfMeasurement,
                NormalRangeMin = model.NormalRangeMin,
                NormalRangeMax = model.NormalRangeMax,
                TurnaroundTimeMinutes = model.TurnaroundTimeMinutes,
                Status = Status.Active
            };

            foreach (var consId in model.SelectedConsumableIds)
            {
                testType.TestTypeConsumables.Add(new TestTypeConsumable { ConsumableId = consId });
            }

            _context.TestTypes.Add(testType);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(TestTypes));
        }




        [HttpGet]
        public async Task<IActionResult> EditTestType(int id)
        {
            var testType = await _context.TestTypes
                .Include(t => t.TestTypeConsumables)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (testType == null) return NotFound();

            var model = new TestTypeViewModel
            {
                Id = testType.Id,
                TestName = testType.TestName,
                TestCategoryId = testType.TestCategoryId,
                SampleTypeId = testType.SampleTypeId,
                UnitsOfMeasurement = testType.UnitsOfMeasurement,
                NormalRangeMin = testType.NormalRangeMin,
                NormalRangeMax = testType.NormalRangeMax,
                TurnaroundTimeMinutes = testType.TurnaroundTimeMinutes,
                SelectedConsumableIds = testType.TestTypeConsumables.Select(tc => tc.ConsumableId).ToList()
            };
            await PopulateDropdowns(activeOnly: true);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTestType(TestTypeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(activeOnly: true);
                return View(model);
            }

            var testType = await _context.TestTypes
                .Include(t => t.TestTypeConsumables)
                .FirstOrDefaultAsync(t => t.Id == model.Id);
            if (testType == null) return NotFound();

            // ✅ Uniqueness check (exclude self)
            bool nameExists = await _context.TestTypes
                .AnyAsync(t => t.TestName == model.TestName && t.Id != model.Id);
            if (nameExists)
            {
                ModelState.AddModelError(nameof(model.TestName), "A test type with this name already exists.");
                await PopulateDropdowns(activeOnly: true);
                return View(model);
            }

            testType.TestName = model.TestName;
            testType.TestCategoryId = model.TestCategoryId;
            testType.SampleTypeId = model.SampleTypeId;
            testType.UnitsOfMeasurement = model.UnitsOfMeasurement;
            testType.NormalRangeMin = model.NormalRangeMin;
            testType.NormalRangeMax = model.NormalRangeMax;
            testType.TurnaroundTimeMinutes = model.TurnaroundTimeMinutes;
            testType.Status = Status.Active;

            _context.TestTypeConsumables.RemoveRange(testType.TestTypeConsumables);
            foreach (var consId in model.SelectedConsumableIds)
            {
                _context.TestTypeConsumables.Add(new TestTypeConsumable { TestTypeId = testType.Id, ConsumableId = consId });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(TestTypes));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTestType(int id)
        {
            var testType = await _context.TestTypes.FindAsync(id);
            if (testType != null)
            {
                testType.Status = Status.Inactive;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(TestTypes));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreTestType(int id)
        {
            var testType = await _context.TestTypes.FindAsync(id);
            if (testType != null)
            {
                testType.Status = Status.Active;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(InactiveTestTypes));
        }

        private async Task PopulateDropdowns(bool activeOnly = true)
        {
            var categoriesQuery = _context.TestCategories.AsQueryable();
            var sampleTypesQuery = _context.SampleTypes.AsQueryable();
            var consumablesQuery = _context.Consumables.AsQueryable();

            if (activeOnly)
            {
                categoriesQuery = categoriesQuery.Where(c => c.Status == Status.Active);
                consumablesQuery = consumablesQuery.Where(c => c.Status == Status.Active);
            }

            ViewBag.Categories = new SelectList(await categoriesQuery.ToListAsync(), "Id", "CategoryName");
            ViewBag.SampleTypes = new SelectList(await sampleTypesQuery.ToListAsync(), "Id", "Name");
            ViewBag.Consumables = await consumablesQuery.ToListAsync();
        }

        #endregion

        #region Consumables & Stock Adjustment (Soft Delete + Restore)

        public async Task<IActionResult> Consumables()
        {
            var consumables = await _context.Consumables
                .Where(c => c.Status == Status.Active)
                .Include(c => c.Supplier)
                .ToListAsync();
            return View(consumables);
        }

        public async Task<IActionResult> InactiveConsumables()
        {
            var consumables = await _context.Consumables
                .Where(c => c.Status == Status.Inactive)
                .Include(c => c.Supplier)
                .ToListAsync();
            return View(consumables);
        }

        [HttpGet]
        public async Task<IActionResult> CreateConsumable()
        {
            ViewBag.Suppliers = new SelectList(
                await _context.Suppliers.Where(s => s.Status == Status.Active).ToListAsync(),
                "Id", "SupplierName");
            return View(new ConsumableViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateConsumable(ConsumableViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Suppliers = new SelectList(
                    await _context.Suppliers.Where(s => s.Status == Status.Active).ToListAsync(),
                    "Id", "SupplierName");
                return View(model);
            }

            // ✅ Uniqueness check for consumable name
            bool nameExists = await _context.Consumables
                .AnyAsync(c => c.ConsumableName == model.ConsumableName);
            if (nameExists)
            {
                ModelState.AddModelError(nameof(model.ConsumableName), "A consumable with this name already exists.");
                ViewBag.Suppliers = new SelectList(
                    await _context.Suppliers.Where(s => s.Status == Status.Active).ToListAsync(),
                    "Id", "SupplierName");
                return View(model);
            }

            var consumable = new Consumable
            {
                ConsumableName = model.ConsumableName,
                ReorderLevel = model.ReorderLevel,
                QuantityOnHand = model.QuantityOnHand,
                SupplierId = model.SupplierId,
                Status = Status.Active
            };
            _context.Consumables.Add(consumable);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Consumables));
        }

        [HttpGet]
        public async Task<IActionResult> EditConsumable(int id)
        {
            var consumable = await _context.Consumables.FindAsync(id);
            if (consumable == null) return NotFound();

            var model = new ConsumableViewModel
            {
                Id = consumable.Id,
                ConsumableName = consumable.ConsumableName,
                ReorderLevel = consumable.ReorderLevel,
                QuantityOnHand = consumable.QuantityOnHand,
                SupplierId = consumable.SupplierId
            };
            ViewBag.Suppliers = new SelectList(
                await _context.Suppliers.Where(s => s.Status == Status.Active).ToListAsync(),
                "Id", "SupplierName");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditConsumable(ConsumableViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Suppliers = new SelectList(
                    await _context.Suppliers.Where(s => s.Status == Status.Active).ToListAsync(),
                    "Id", "SupplierName");
                return View(model);
            }

            var consumable = await _context.Consumables.FindAsync(model.Id);
            if (consumable == null) return NotFound();

            // ✅ Uniqueness check (exclude self)
            bool nameExists = await _context.Consumables
                .AnyAsync(c => c.ConsumableName == model.ConsumableName && c.Id != model.Id);
            if (nameExists)
            {
                ModelState.AddModelError(nameof(model.ConsumableName), "A consumable with this name already exists.");
                ViewBag.Suppliers = new SelectList(
                    await _context.Suppliers.Where(s => s.Status == Status.Active).ToListAsync(),
                    "Id", "SupplierName");
                return View(model);
            }

            consumable.ConsumableName = model.ConsumableName;
            consumable.ReorderLevel = model.ReorderLevel;
            consumable.QuantityOnHand = model.QuantityOnHand;
            consumable.SupplierId = model.SupplierId;
            consumable.Status = Status.Active;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Consumables));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConsumable(int id)
        {
            // Guard 1: don't orphan active test types that require this consumable
            bool usedByTestType = await _context.TestTypeConsumables
                .AnyAsync(tc => tc.ConsumableId == id && tc.TestType.Status == Status.Active);
            if (usedByTestType)
            {
                SetError("This consumable is used by one or more active test types and cannot be deactivated.");
                return RedirectToAction(nameof(Consumables));
            }

            // Guard 2: don't deactivate a consumable that's part of an order still being processed
            bool onOpenOrder = await _context.OrderItems
                .AnyAsync(oi => oi.ConsumableId == id
                             && oi.Status == Status.Active
                             && oi.OrderItemStatus == OrderItemStatus.Ordered);
            if (onOpenOrder)
            {
                SetError("This consumable appears on one or more outstanding orders and cannot be deactivated until those orders are received or cancelled.");
                return RedirectToAction(nameof(Consumables));
            }

            var consumable = await _context.Consumables.FindAsync(id);
            if (consumable != null)
            {
                consumable.Status = Status.Inactive;
                await _context.SaveChangesAsync();
                SetSuccess("Consumable deactivated successfully.");
            }
            return RedirectToAction(nameof(Consumables));
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreConsumable(int id)
        {
            var consumable = await _context.Consumables.FindAsync(id);
            if (consumable != null)
            {
                consumable.Status = Status.Active;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(InactiveConsumables));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustStock(StockAdjustmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(" ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                SetError(string.IsNullOrWhiteSpace(errors) ? "Invalid stock adjustment." : errors);
                return RedirectToAction(nameof(Consumables));
            }

            var consumable = await _context.Consumables
                .FirstOrDefaultAsync(c => c.Id == model.ConsumableId && c.Status == Status.Active);
            if (consumable == null)
            {
                SetError("Consumable not found or is inactive.");
                return RedirectToAction(nameof(Consumables));
            }

            int previousQuantity = consumable.QuantityOnHand;

            switch (model.AdjustmentType)
            {
                case "Increase":
                    consumable.QuantityOnHand += model.Quantity;
                    break;

                case "Decrease":
                    if (model.Quantity > consumable.QuantityOnHand)
                    {
                        SetError($"Cannot decrease by {model.Quantity}. Only {consumable.QuantityOnHand} units are currently on hand.");
                        return RedirectToAction(nameof(Consumables));
                    }
                    consumable.QuantityOnHand -= model.Quantity;
                    break;

                case "Set":
                    consumable.QuantityOnHand = model.Quantity;
                    break;

                default:
                    SetError("Unrecognized adjustment type.");
                    return RedirectToAction(nameof(Consumables));
            }

            await _context.SaveChangesAsync();

            SetSuccess(
                $"Stock for '{consumable.ConsumableName}' adjusted from {previousQuantity} to {consumable.QuantityOnHand}. Reason: {model.Reason}");

            return RedirectToAction(nameof(Consumables));
        }


        #endregion

        #region Suppliers (Soft Delete + Restore)

        public async Task<IActionResult> Suppliers()
        {
            var suppliers = await _context.Suppliers
                .Where(s => s.Status == Status.Active)
                .ToListAsync();
            return View(suppliers);
        }

        public async Task<IActionResult> InactiveSuppliers()
        {
            var suppliers = await _context.Suppliers
                .Where(s => s.Status == Status.Inactive)
                .ToListAsync();
            return View(suppliers);
        }

        [HttpGet]
        public IActionResult CreateSupplier() => View(new SupplierViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSupplier(SupplierViewModel model)
        {
            bool nameExists = await _context.Suppliers
                .AnyAsync(c => c.SupplierName == model.SupplierName);
            if (nameExists)
            {
                ModelState.AddModelError(nameof(model.SupplierName), "A supplier with this name already exists.");
                ViewBag.Suppliers = new SelectList(
                    await _context.Suppliers.Where(s => s.Status == Status.Active).ToListAsync(),
                    "Id", "SupplierName");
                return View(model);
            }

            bool emailExists = await _context.Suppliers
                .AnyAsync(c => c.EmailAddress == model.EmailAddress);
            if (emailExists)
            {
                ModelState.AddModelError(nameof(model.EmailAddress), "A supplier with this email address already exists.");
                ViewBag.Suppliers = new SelectList(
                    await _context.Suppliers.Where(s => s.Status == Status.Active).ToListAsync(),
                    "Id", "SupplierName");
                return View(model);
            }


            if (!ModelState.IsValid) return View(model);

            var supplier = new Supplier
            {
                SupplierName = model.SupplierName,
                ContactPerson = model.ContactPerson,
                EmailAddress = model.EmailAddress,
                Status = Status.Active
            };
            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Suppliers));
        }

        [HttpGet]
        public async Task<IActionResult> EditSupplier(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound();

            var model = new SupplierViewModel
            {
                Id = supplier.Id,
                SupplierName = supplier.SupplierName,
                ContactPerson = supplier.ContactPerson,
                EmailAddress = supplier.EmailAddress
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSupplier(SupplierViewModel model)
        {

            bool nameExists = await _context.Suppliers
                .AnyAsync(c => c.SupplierName == model.SupplierName);
            if (nameExists)
            {
                ModelState.AddModelError(nameof(model.SupplierName), "A supplier with this name already exists.");
                ViewBag.Suppliers = new SelectList(
                    await _context.Suppliers.Where(s => s.Status == Status.Active).ToListAsync(),
                    "Id", "SupplierName");
                return View(model);
            }

            bool emailExists = await _context.Suppliers
                .AnyAsync(c => c.EmailAddress == model.EmailAddress);
            if (emailExists)
            {
                ModelState.AddModelError(nameof(model.EmailAddress), "A supplier with this email address already exists.");
                ViewBag.Suppliers = new SelectList(
                    await _context.Suppliers.Where(s => s.Status == Status.Active).ToListAsync(),
                    "Id", "SupplierName");
                return View(model);
            }


            if (!ModelState.IsValid) return View(model);

            var supplier = await _context.Suppliers.FindAsync(model.Id);
            if (supplier == null) return NotFound();

            supplier.SupplierName = model.SupplierName;
            supplier.ContactPerson = model.ContactPerson;
            supplier.EmailAddress = model.EmailAddress;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Suppliers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            // Guard 1: don't orphan active consumables that are still sourced from this supplier
            bool hasActiveConsumables = await _context.Consumables
                .AnyAsync(c => c.SupplierId == id && c.Status == Status.Active);
            if (hasActiveConsumables)
            {
                SetError("This supplier has one or more active consumables assigned and cannot be deactivated.");
                return RedirectToAction(nameof(Suppliers));
            }

            // Guard 2: don't deactivate a supplier with an order still outstanding
            bool hasOpenOrders = await _context.Orders
                .AnyAsync(o => o.SupplierId == id
                             && o.Status == Status.Active
                             && (o.OrderStatus == OrderStatus.Ordered || o.OrderStatus == OrderStatus.PartiallyComplete));
            if (hasOpenOrders)
            {
                SetError("This supplier has one or more outstanding orders and cannot be deactivated until those orders are completed or cancelled.");
                return RedirectToAction(nameof(Suppliers));
            }

            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier != null)
            {
                supplier.Status = Status.Inactive;
                await _context.SaveChangesAsync();
                SetSuccess("Supplier deactivated successfully.");
            }
            return RedirectToAction(nameof(Suppliers));
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreSupplier(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier != null)
            {
                supplier.Status = Status.Active;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(InactiveSuppliers));
        }

        #endregion

        #region Orders (Soft Delete + Cancel + Restore)

        public async Task<IActionResult> LowStockAlert()
        {
            var threshold = 0.1m;
            var lowStockItems = await _context.Consumables
                .Include(c => c.Supplier)
                .Where(c => c.Status == Status.Active)
                .Where(c => c.QuantityOnHand <= c.ReorderLevel * (1 + threshold))
                .ToListAsync();
            return View(lowStockItems);
        }

        [HttpGet]
        public async Task<IActionResult> CreateOrder()
        {
            var lowStock = await _context.Consumables
                .Include(c => c.Supplier)
                .Where(c => c.Status == Status.Active)
                .Where(c => c.QuantityOnHand <= c.ReorderLevel * 1.1m)   // consistent with alert
                .GroupBy(c => c.Supplier)
                .ToListAsync();

            var model = new OrderCreateViewModel();
            ViewBag.LowStockBySupplier = lowStock;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrder(OrderCreateViewModel model)
        {
            if (model.ItemQuantities == null || !model.ItemQuantities.Any())
            {
                ModelState.AddModelError("", "Please select at least one item to order.");
                return View(model);
            }

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.Id == model.SupplierId && s.Status == Status.Active);
            if (supplier == null) return NotFound();

            string orderNumber = await GenerateUniqueOrderNumberAsync();

            var order = new Order
            {
                OrderNumber = orderNumber,
                SupplierId = model.SupplierId,
                OrderDate = DateTime.Now,
                OrderStatus = OrderStatus.Ordered,
                Status = Status.Active
            };

            foreach (var item in model.ItemQuantities.Where(kv => kv.Value > 0))
            {
                order.OrderItems.Add(new OrderItem
                {
                    ConsumableId = item.Key,
                    QuantityOrdered = item.Value,
                    OrderItemStatus = OrderItemStatus.Ordered,
                    Status = Status.Active
                });
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(supplier.EmailAddress))
            {
                string body = $"Dear {supplier.ContactPerson ?? supplier.SupplierName},\n\n" +
                              $"Please find attached order #{orderNumber}.\n\nItems:\n";
                foreach (var oi in order.OrderItems)
                {
                    var cons = await _context.Consumables.FindAsync(oi.ConsumableId);
                    body += $"- {cons?.ConsumableName}: {oi.QuantityOrdered}\n";
                }
                await _emailService.SendEmailAsync(supplier.EmailAddress, $"New Order #{orderNumber}", body);
            }

            SetSuccess($"Order #{orderNumber} created successfully.");
            return RedirectToAction(nameof(Orders));
        }




        public async Task<IActionResult> Orders()
        {
            var orders = await _context.Orders
                .Where(o => o.Status == Status.Active)
                .Include(o => o.Supplier)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Consumable)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return View(orders);
        }

        public async Task<IActionResult> InactiveOrders()
        {
            var orders = await _context.Orders
                .Where(o => o.Status == Status.Inactive)
                .Include(o => o.Supplier)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Consumable)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkOrderReceived(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.Status == Status.Active);
            if (order == null) return NotFound();

            foreach (var item in order.OrderItems.Where(i => i.OrderItemStatus == OrderItemStatus.Ordered))
            {
                item.OrderItemStatus = OrderItemStatus.Received;
                item.DateReceived = DateTime.Now;

                var consumable = await _context.Consumables.FindAsync(item.ConsumableId);
                if (consumable != null)
                    consumable.QuantityOnHand += item.QuantityOrdered;
            }

            if (order.OrderItems.All(i => i.OrderItemStatus == OrderItemStatus.Received))
            {
                order.OrderStatus = OrderStatus.Complete;
                order.DateCompleted = DateTime.Now;                 // ✅ capture completion date
            }
            else if (order.OrderItems.Any(i => i.OrderItemStatus == OrderItemStatus.Received))
                order.OrderStatus = OrderStatus.PartiallyComplete;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Orders));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int orderId, string cancellationReason)
        {
            var order = await _context.Orders
                .Include(o => o.Supplier)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.Status == Status.Active);
            if (order == null) return NotFound();

            order.OrderStatus = OrderStatus.Cancelled;
            order.DateCancelled = DateTime.Now;
            order.CancellationReason = cancellationReason;

            foreach (var item in order.OrderItems.Where(i => i.OrderItemStatus != OrderItemStatus.Received))
            {
                item.OrderItemStatus = OrderItemStatus.Cancelled;
                item.DateCancelled = DateTime.Now;
                item.CancellationReason = "Order cancelled";
            }

            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(order.Supplier?.EmailAddress))
            {
                await _emailService.SendEmailAsync(order.Supplier.EmailAddress,
                    $"Order #{order.OrderNumber} Cancelled",
                    $"Dear {order.Supplier.ContactPerson ?? order.Supplier.SupplierName},\n\n" +
                    $"Order #{order.OrderNumber} has been cancelled.\nReason: {cancellationReason}");
            }

            return RedirectToAction(nameof(Orders));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrderItem(int orderItemId, string cancellationReason)
        {
            var orderItem = await _context.OrderItems
                .Include(oi => oi.Order).ThenInclude(o => o.Supplier)
                .FirstOrDefaultAsync(oi => oi.Id == orderItemId && oi.Status == Status.Active);
            if (orderItem == null) return NotFound();

            orderItem.OrderItemStatus = OrderItemStatus.Cancelled;
            orderItem.DateCancelled = DateTime.Now;
            orderItem.CancellationReason = cancellationReason;

            var order = orderItem.Order;
            if (order.OrderItems.All(i => i.OrderItemStatus == OrderItemStatus.Received || i.OrderItemStatus == OrderItemStatus.Cancelled))
            {
                if (order.OrderItems.Any(i => i.OrderItemStatus == OrderItemStatus.Received))
                    order.OrderStatus = OrderStatus.PartiallyComplete;
                else
                    order.OrderStatus = OrderStatus.Cancelled;
            }

            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(order.Supplier?.EmailAddress))
            {
                await _emailService.SendEmailAsync(order.Supplier.EmailAddress,
                    $"Order #{order.OrderNumber} – Item Cancelled",
                    $"Item {orderItem.Consumable?.ConsumableName} has been cancelled.\nReason: {cancellationReason}");
            }

            return RedirectToAction(nameof(Orders));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                order.Status = Status.Inactive;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Orders));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                order.Status = Status.Active;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(InactiveOrders));
        }

        [HttpGet]
        public async Task<IActionResult> EditOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Supplier)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Consumable)
                .FirstOrDefaultAsync(o => o.Id == id && o.Status == Status.Active);

            if (order == null) return NotFound();

            if (order.OrderStatus != OrderStatus.Ordered)
            {
                SetError("Cannot edit an order that has already been processed.");
                return RedirectToAction(nameof(Orders));
            }

            var model = new EditOrderViewModel
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                SupplierId = order.SupplierId,
                SupplierName = order.Supplier.SupplierName,
                OrderDate = order.OrderDate,
                OrderStatus = order.OrderStatus,
                Items = order.OrderItems.Select(oi => new OrderItemEditModel
                {
                    OrderItemId = oi.Id,
                    ConsumableId = oi.ConsumableId,
                    ConsumableName = oi.Consumable.ConsumableName,
                    QuantityOrdered = oi.QuantityOrdered,
                    Status = oi.OrderItemStatus,
                    Remove = false
                }).ToList()
            };

            ViewBag.AvailableConsumables = new SelectList(
                await _context.Consumables
                    .Where(c => c.SupplierId == order.SupplierId && c.Status == Status.Active)
                    .ToListAsync(),
                "Id", "ConsumableName");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditOrder(EditOrderViewModel model)
        {
            if (model.Items == null || !model.Items.Any(i => !i.Remove))
            {
                ModelState.AddModelError("", "Order must contain at least one item.");
            }

            if (!ModelState.IsValid)
            {
                var orderForSupplier = await _context.Orders.FindAsync(model.OrderId);
                ViewBag.AvailableConsumables = new SelectList(
                    await _context.Consumables
                        .Where(c => c.SupplierId == orderForSupplier.SupplierId && c.Status == Status.Active)
                        .ToListAsync(),
                    "Id", "ConsumableName");
                return View(model);
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == model.OrderId && o.Status == Status.Active);

            if (order == null) return NotFound();
            if (order.OrderStatus != OrderStatus.Ordered)
            {
                SetError("Cannot edit an order that has already been processed.");
                return RedirectToAction(nameof(Orders));
            }

            foreach (var itemModel in model.Items)
            {
                var existingItem = order.OrderItems.FirstOrDefault(oi => oi.Id == itemModel.OrderItemId);
                if (existingItem == null) continue;

                if (itemModel.Remove)
                {
                    _context.OrderItems.Remove(existingItem);
                }
                else
                {
                    if (existingItem.OrderItemStatus == OrderItemStatus.Ordered)
                    {
                        existingItem.QuantityOrdered = itemModel.QuantityOrdered;
                    }
                }
            }

            if (model.NewConsumableId.HasValue && model.NewQuantity.HasValue && model.NewQuantity.Value > 0)
            {
                var consumable = await _context.Consumables.FindAsync(model.NewConsumableId.Value);
                if (consumable != null && consumable.Status == Status.Active && consumable.SupplierId == order.SupplierId)
                {
                    var newItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ConsumableId = model.NewConsumableId.Value,
                        QuantityOrdered = model.NewQuantity.Value,
                        OrderItemStatus = OrderItemStatus.Ordered,
                        Status = Status.Active
                    };
                    _context.OrderItems.Add(newItem);
                }
            }

            await _context.SaveChangesAsync();

            SetSuccess("Order updated successfully.");
            return RedirectToAction(nameof(Orders));
        }

        #endregion

        #region Doctor Management (Soft Delete + Restore + Edit)

        public async Task<IActionResult> Doctors()
        {
            var doctors = await _context.Employees
                .Where(e => e.Role == UserRole.Doctor && e.IsActive == Status.Active)
                .ToListAsync();
            return View(doctors);
        }

        public async Task<IActionResult> InactiveDoctors()
        {
            var doctors = await _context.Employees
                .Where(e => e.Role == UserRole.Doctor && e.IsActive == Status.Inactive)
                .ToListAsync();
            return View(doctors);
        }

        [HttpGet]
        public IActionResult CreateDoctor() => View(new DoctorUserViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDoctor(DoctorUserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _context.Employees.AnyAsync(e => e.HPCSANumber == model.HPCSANumber))
            {
                ModelState.AddModelError(nameof(model.HPCSANumber), "HPCSA number already registered.");
                return View(model);
            }

            if (await _context.Employees.AnyAsync(e => e.Email == model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "Email address already registered.");
                return View(model);
            }

            string tempPassword = GenerateRandomPassword();
            var doctor = new Employee
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Username = model.Email,
                HPCSANumber = model.HPCSANumber,
                ContactNumber = model.ContactNumber,
                Role = UserRole.Doctor,
                IsActive = Status.Active,
                MustChangePassword = true,
                PasswordHash = HashPassword(tempPassword)
            };

            _context.Employees.Add(doctor);
            await _context.SaveChangesAsync();

            await _emailService.SendEmailAsync(doctor.Email, "Your NMB-HLabSys Account",
                $"Dear Dr. {doctor.LastName},\n\n" +
                $"Your account has been created.\n" +
                $"Username (email): {doctor.Email}\n" +
                $"Temporary Password: {tempPassword}\n\n" +
                $"Please log in and change your password.");

            await _notificationService.CreateAsync(doctor.Id, "Doctor",
                $"Welcome, Dr. {doctor.LastName}! Your account has been created. Please log in and explore the NMB-HLabSys platform. If you have any questions, the lab team is here to assist.",
                "/Doctor/Dashboard");

            return RedirectToAction(nameof(Doctors));
        }

        [HttpGet]
        public async Task<IActionResult> EditDoctor(int id)
        {
            var doctor = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == id && e.Role == UserRole.Doctor);
            if (doctor == null) return NotFound();

            var model = new DoctorUserViewModel
            {
                Id = doctor.Id,
                FirstName = doctor.FirstName,
                LastName = doctor.LastName,
                HPCSANumber = doctor.HPCSANumber,
                Email = doctor.Email,
                ContactNumber = doctor.ContactNumber
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDoctor(DoctorUserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var doctor = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == model.Id && e.Role == UserRole.Doctor);
            if (doctor == null) return NotFound();

            if (await _context.Employees.AnyAsync(e => e.HPCSANumber == model.HPCSANumber && e.Id != model.Id))
            {
                ModelState.AddModelError(nameof(model.HPCSANumber), "HPCSA number already registered.");
                return View(model);
            }

            if (await _context.Employees.AnyAsync(e => e.Email == model.Email && e.Id != model.Id))
            {
                ModelState.AddModelError(nameof(model.Email), "Email address already registered.");
                return View(model);
            }

            doctor.FirstName = model.FirstName;
            doctor.LastName = model.LastName;
            doctor.HPCSANumber = model.HPCSANumber;
            doctor.Email = model.Email;
            doctor.Username = model.Email;
            doctor.ContactNumber = model.ContactNumber;
            doctor.IsActive = Status.Active;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Doctors));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDoctor(int id)
        {
            var doctor = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == id && e.Role == UserRole.Doctor);
            if (doctor != null)
            {
                doctor.IsActive = Status.Inactive;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Doctors));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreDoctor(int id)
        {
            var doctor = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == id && e.Role == UserRole.Doctor);
            if (doctor != null)
            {
                doctor.IsActive = Status.Active;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(InactiveDoctors));
        }

        #endregion

        #region Lab Technician Management (Soft Delete + Restore + Edit)

        public async Task<IActionResult> Technicians()
        {
            var techs = await _context.Employees
                .Include(e => e.TechnicianTestTypes).ThenInclude(tt => tt.TestType)
                .Where(e => e.Role == UserRole.LabTechnician && e.IsActive == Status.Active)
                .ToListAsync();
            return View(techs);
        }

        public async Task<IActionResult> InactiveTechnicians()
        {
            var techs = await _context.Employees
                .Include(e => e.TechnicianTestTypes).ThenInclude(tt => tt.TestType)
                .Where(e => e.Role == UserRole.LabTechnician && e.IsActive == Status.Inactive)
                .ToListAsync();
            return View(techs);
        }

        [HttpGet]
        public async Task<IActionResult> CreateTechnician()
        {
            ViewBag.TestTypes = await _context.TestTypes
                .Where(t => t.Status == Status.Active)
                .ToListAsync();
            return View(new LabTechnicianViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTechnician(LabTechnicianViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.TestTypes = await _context.TestTypes.Where(t => t.Status == Status.Active).ToListAsync();
                return View(model);
            }

            if (await _context.Employees.AnyAsync(e => e.SAIDNumber == model.SAIDNumber))
            {
                ModelState.AddModelError(nameof(model.SAIDNumber), "ID number already registered.");
                ViewBag.TestTypes = await _context.TestTypes.Where(t => t.Status == Status.Active).ToListAsync();
                return View(model);
            }

            if (await _context.Employees.AnyAsync(e => e.Email == model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "Email address already registered.");
                ViewBag.TestTypes = await _context.TestTypes.Where(t => t.Status == Status.Active).ToListAsync();
                return View(model);
            }

            if (model.SelectedTestTypeIds == null || model.SelectedTestTypeIds.Count == 0)
            {
                ModelState.AddModelError("", "At least one test type must be assigned.");
                ViewBag.TestTypes = await _context.TestTypes.Where(t => t.Status == Status.Active).ToListAsync();
                return View(model);
            }

            string tempPassword = GenerateRandomPassword();
            var tech = new Employee
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Username = model.Email,
                SAIDNumber = model.SAIDNumber,
                EmployeeNumber = model.EmployeeNumber,
                ContactNumber = model.ContactNumber,
                Role = UserRole.LabTechnician,
                IsActive = Status.Active,
                MustChangePassword = true,
                PasswordHash = HashPassword(tempPassword)
            };

            foreach (var ttId in model.SelectedTestTypeIds)
            {
                tech.TechnicianTestTypes.Add(new TechnicianTestType { TestTypeId = ttId });
            }

            _context.Employees.Add(tech);
            await _context.SaveChangesAsync();

            await _emailService.SendEmailAsync(tech.Email, "Your NMB-HLabSys Account",
                $"Dear {tech.FirstName},\n\n" +
                $"Your technician account has been created.\n" +
                $"Username (email): {tech.Email}\n" +
                $"Temporary Password: {tempPassword}\n\n" +
                $"Please log in and change your password.");


            await _notificationService.CreateAsync(tech.Id, "LabTechnician",
                $"Welcome aboard, {tech.FirstName}! Your lab technician account is now active. You can start processing samples right away. Glad to have you on the team!",
                "/LabTechnician/Dashboard");

            return RedirectToAction(nameof(Technicians));
        }

        [HttpGet]
        public async Task<IActionResult> EditTechnician(int id)
        {
            var tech = await _context.Employees
                .Include(e => e.TechnicianTestTypes)
                .FirstOrDefaultAsync(e => e.Id == id && e.Role == UserRole.LabTechnician);
            if (tech == null) return NotFound();

            var model = new LabTechnicianViewModel
            {
                Id = tech.Id,
                FirstName = tech.FirstName,
                LastName = tech.LastName,
                SAIDNumber = tech.SAIDNumber,
                EmployeeNumber = tech.EmployeeNumber,
                Email = tech.Email,
                ContactNumber = tech.ContactNumber,
                SelectedTestTypeIds = tech.TechnicianTestTypes.Select(tt => tt.TestTypeId).ToList()
            };
            ViewBag.TestTypes = await _context.TestTypes.Where(t => t.Status == Status.Active).ToListAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTechnician(LabTechnicianViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.TestTypes = await _context.TestTypes.Where(t => t.Status == Status.Active).ToListAsync();
                return View(model);
            }

            var tech = await _context.Employees
                .Include(e => e.TechnicianTestTypes)
                .FirstOrDefaultAsync(e => e.Id == model.Id && e.Role == UserRole.LabTechnician);
            if (tech == null) return NotFound();

            if (await _context.Employees.AnyAsync(e => e.SAIDNumber == model.SAIDNumber && e.Id != model.Id))
            {
                ModelState.AddModelError(nameof(model.SAIDNumber), "ID number already registered.");
                ViewBag.TestTypes = await _context.TestTypes.Where(t => t.Status == Status.Active).ToListAsync();
                return View(model);
            }

            if (await _context.Employees.AnyAsync(e => e.Email == model.Email && e.Id != model.Id))
            {
                ModelState.AddModelError(nameof(model.Email), "Email address already registered.");
                ViewBag.TestTypes = await _context.TestTypes.Where(t => t.Status == Status.Active).ToListAsync();
                return View(model);
            }

            if (model.SelectedTestTypeIds == null || model.SelectedTestTypeIds.Count == 0)
            {
                ModelState.AddModelError("", "At least one test type must be assigned.");
                ViewBag.TestTypes = await _context.TestTypes.Where(t => t.Status == Status.Active).ToListAsync();
                return View(model);
            }

            tech.FirstName = model.FirstName;
            tech.LastName = model.LastName;
            tech.SAIDNumber = model.SAIDNumber;
            tech.EmployeeNumber = model.EmployeeNumber;
            tech.Email = model.Email;
            tech.Username = model.Email;
            tech.ContactNumber = model.ContactNumber;
            tech.IsActive = model.IsActive;

            _context.TechnicianTestTypes.RemoveRange(tech.TechnicianTestTypes);
            foreach (var ttId in model.SelectedTestTypeIds)
            {
                _context.TechnicianTestTypes.Add(new TechnicianTestType { TechnicianId = tech.Id, TestTypeId = ttId });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Technicians));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTechnician(int id)
        {
            var tech = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == id && e.Role == UserRole.LabTechnician);
            if (tech != null)
            {
                tech.IsActive = Status.Inactive;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Technicians));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreTechnician(int id)
        {
            var tech = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == id && e.Role == UserRole.LabTechnician);
            if (tech != null)
            {
                tech.IsActive = Status.Active;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(InactiveTechnicians));
        }

        #endregion


        #region Sample Types (Soft Delete + Restore)

        public async Task<IActionResult> SampleTypes()
        {
            var sampleTypes = await _context.SampleTypes
                .Where(st => st.Status == Status.Active)
                .ToListAsync();
            return View(sampleTypes);
        }

        public async Task<IActionResult> InactiveSampleTypes()
        {
            var sampleTypes = await _context.SampleTypes
                .Where(st => st.Status == Status.Inactive)
                .ToListAsync();
            return View(sampleTypes);
        }

        [HttpGet]
        public IActionResult CreateSampleType() => View(new SampleTypeViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSampleType(SampleTypeViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // ✅ Uniqueness check
            bool nameExists = await _context.SampleTypes
                .AnyAsync(st => st.Name == model.Name);
            if (nameExists)
            {
                ModelState.AddModelError(nameof(model.Name), "A sample type with this name already exists.");
                return View(model);
            }

            var sampleType = new SampleType
            {
                Name = model.Name,
                Status = Status.Active
            };
            _context.SampleTypes.Add(sampleType);
            await _context.SaveChangesAsync();

            SetSuccess("Sample type created successfully.");
            return RedirectToAction(nameof(SampleTypes));
        }

        [HttpGet]
        public async Task<IActionResult> EditSampleType(int id)
        {
            var sampleType = await _context.SampleTypes.FindAsync(id);
            if (sampleType == null) return NotFound();

            var model = new SampleTypeViewModel
            {
                Id = sampleType.Id,
                Name = sampleType.Name
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSampleType(SampleTypeViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var sampleType = await _context.SampleTypes.FindAsync(model.Id);
            if (sampleType == null) return NotFound();

            // ✅ Uniqueness check (exclude self)
            bool nameExists = await _context.SampleTypes
                .AnyAsync(st => st.Name == model.Name && st.Id != model.Id);
            if (nameExists)
            {
                ModelState.AddModelError(nameof(model.Name), "A sample type with this name already exists.");
                return View(model);
            }

            sampleType.Name = model.Name;
            sampleType.Status = Status.Active;
            await _context.SaveChangesAsync();

            SetSuccess("Sample type updated successfully.");
            return RedirectToAction(nameof(SampleTypes));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSampleType(int id)
        {
            // Guard: don't let a manager silently orphan test types that reference this sample type
            bool inUse = await _context.TestTypes
                .AnyAsync(t => t.SampleTypeId == id && t.Status == Status.Active);
            if (inUse)
            {
                SetError("This sample type is used by one or more active test types and cannot be deactivated.");
                return RedirectToAction(nameof(SampleTypes));
            }

            var sampleType = await _context.SampleTypes.FindAsync(id);
            if (sampleType != null)
            {
                sampleType.Status = Status.Inactive;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(SampleTypes));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreSampleType(int id)
        {
            var sampleType = await _context.SampleTypes.FindAsync(id);
            if (sampleType != null)
            {
                sampleType.Status = Status.Active;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(InactiveSampleTypes));
        }

        #endregion



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkOrderItemReceived(int orderItemId)
        {
            var orderItem = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Consumable)
                .FirstOrDefaultAsync(oi => oi.Id == orderItemId && oi.Status == Status.Active);

            if (orderItem == null)
            {
                SetError("Order item not found.");
                return RedirectToAction(nameof(Orders));
            }

            if (orderItem.OrderItemStatus != OrderItemStatus.Ordered)
            {
                SetError($"This item is already marked as {orderItem.OrderItemStatus} and cannot be received again.");
                return RedirectToAction(nameof(Orders));
            }

            var order = orderItem.Order;
            if (order == null || order.Status != Status.Active)
            {
                SetError("The parent order could not be found or is inactive.");
                return RedirectToAction(nameof(Orders));
            }

            if (order.OrderStatus == OrderStatus.Cancelled)
            {
                SetError("Cannot receive items on a cancelled order.");
                return RedirectToAction(nameof(Orders));
            }

            // Receive this item only
            orderItem.OrderItemStatus = OrderItemStatus.Received;
            orderItem.DateReceived = DateTime.Now;

            if (orderItem.Consumable != null)
            {
                orderItem.Consumable.QuantityOnHand += orderItem.QuantityOrdered;
            }

            // Recompute the parent order's status based on all of its items
            var allItems = await _context.OrderItems
                .Where(oi => oi.OrderId == order.Id && oi.Status == Status.Active)
                .ToListAsync();

            if (allItems.All(i => i.OrderItemStatus == OrderItemStatus.Received))
            {
                order.OrderStatus = OrderStatus.Complete;
                order.DateCompleted = DateTime.Now;
            }
            else if (allItems.Any(i => i.OrderItemStatus == OrderItemStatus.Received))
            {
                order.OrderStatus = OrderStatus.PartiallyComplete;
            }
            // If none received yet (shouldn't happen here since we just received one),
            // status stays as-is.

            await _context.SaveChangesAsync();

            SetSuccess($"'{orderItem.Consumable?.ConsumableName}' marked as received. Stock updated.");
            return RedirectToAction(nameof(Orders));
        }


        #region Reports

        [HttpGet]
        public IActionResult TestPerformanceReport() => View(new ReportDateRangeViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TestPerformanceReport(ReportDateRangeViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var pdfBytes = await _pdfService.GenerateTestPerformanceReport(model.StartDate, model.EndDate);
            return File(pdfBytes, "application/pdf", $"TestPerformance_{model.StartDate:yyyyMMdd}-{model.EndDate:yyyyMMdd}.pdf");
        }

        #endregion

        #region Helpers

        private static string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);

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
                byte[] uintBuffer = new byte[sizeof(uint)];
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

        #endregion

        private async Task<string> GenerateUniqueOrderNumberAsync()
        {
            const int maxAttempts = 5;
            string orderNumber;
            bool exists;
            int attempts = 0;

            do
            {
                orderNumber = "ORD-" + DateTime.Now.ToString("yyyyMMdd") + "-"
                             + Guid.NewGuid().ToString().Substring(0, 4).ToUpper();

                exists = await _context.Orders.AnyAsync(o => o.OrderNumber == orderNumber);
                attempts++;

                if (attempts >= maxAttempts && exists)
                {
                    // Vanishingly unlikely, but fall back to a fragment long enough
                    // that collision is practically impossible, and stop looping.
                    orderNumber = "ORD-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-"
                                 + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
                    exists = await _context.Orders.AnyAsync(o => o.OrderNumber == orderNumber);
                }
            }
            while (exists && attempts < maxAttempts);

            return orderNumber;
        }


        [HttpGet]
        public async Task<IActionResult> NotifyOverdueTests()
        {
            var now = DateTime.Now;

            // Find all in‑progress tests that are overdue (not paused, past expected completion)
            var overdueTests = await _context.TestRequestTestTypes
                .Include(trt => trt.TestRequest).ThenInclude(tr => tr.Patient)
                .Include(trt => trt.TestType)
                .Where(trt => trt.RequestStatus == RequestStatus.InProgress
                              && trt.StartDateTime.HasValue
                              && !trt.IsPaused
                              && now > trt.StartDateTime.Value
                                        .AddMinutes(trt.TestType.TurnaroundTimeMinutes)
                                        .Add(trt.AccumulatedPauseTime))
                .ToListAsync();

            if (!overdueTests.Any())
            {
                SetSuccess("No overdue tests found.");
                return RedirectToAction(nameof(DashBoard));
            }

            // Get all lab managers (assuming Role "LabManager" – adjust if needed)
            var managers = await _context.Employees
                .Where(e => e.Role == UserRole.LaboratoryManager && e.IsActive == Status.Active)
                .ToListAsync();

            foreach (var test in overdueTests)
            {
                foreach (var manager in managers)
                {
                    await _notificationService.CreateAsync(
                        manager.Id,
                        "LabManager",
                        $"Overdue Test: {test.TestType.TestName} for patient {test.TestRequest.Patient?.FirstName} " +
                        $"in request #{test.TestRequestId}. Expected completion was " +
                        $"{test.StartDateTime!.Value.AddMinutes(test.TestType.TurnaroundTimeMinutes).Add(test.AccumulatedPauseTime):g}.",
                        $"/LabTechnician/ProcessTestTypes?requestId={test.TestRequestId}"
                    );
                }
            }

            SetSuccess($"Sent overdue alerts for {overdueTests.Count} test(s) to {managers.Count} manager(s).");
            return RedirectToAction(nameof(DashBoard));
        }



    }
}