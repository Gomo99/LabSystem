using LaboratoryTestRequestManagementSystem.AppStatus;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class AdminFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public string? Category { get; set; }
        public bool ShowInactive { get; set; } = false;
    }

    public class AlertsFilterViewModel
    {
        public DateTime StartDate { get; set; } = DateTime.Today.AddDays(-5);
        public DateTime EndDate { get; set; } = DateTime.Today;
        public List<AlertViewModel> Alerts { get; set; } = new();
    }


    public class AlertViewModel
    {
        public int TestRequestId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string TestName { get; set; } = string.Empty;
        public string ResultValue { get; set; } = string.Empty;
        public string? NormalRange { get; set; }
        public DateTime? CompletedDate { get; set; }
    }

    public class AllergyViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Allergy Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Category")]
        public string? Category { get; set; }
    }


    public class AvailableTestTypeViewModel
    {
        public int TestRequestId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public Urgency Urgency { get; set; }
        public string? ClinicalNotes { get; set; }  // ✅ Doctor's clinical notes

        // ✅ Patient medical history
        public List<string> MedicalConditions { get; set; } = new();
        public List<string> Allergies { get; set; } = new();
        public List<string> Medications { get; set; } = new();

        public List<TestTypeItemForProcessingViewModel> TestTypes { get; set; } = new();
    }


    public class CaptureResultViewModel
    {
        public int TestRequestId { get; set; }
        public int TestTypeId { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string UnitsOfMeasurement { get; set; } = string.Empty;
        public decimal? NormalRangeMin { get; set; }
        public decimal? NormalRangeMax { get; set; }

        // ✅ Patient medical history and clinical notes
        public string? ClinicalNotes { get; set; }
        public List<string> MedicalConditions { get; set; } = new();
        public List<string> Allergies { get; set; } = new();
        public List<string> Medications { get; set; } = new();

        [Required(ErrorMessage = "Result value is required.")]
        [Display(Name = "Result Value")]
        public string ResultValue { get; set; } = string.Empty;

        [Display(Name = "Notes")]
        public string? Notes { get; set; }
    }


    public class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[^\w\s]).{8,}$",
            ErrorMessage = "Password must be at least 8 characters and contain at least one uppercase letter, one number, and one special character.")]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }


    public class ChangeUsernameViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "New Email Address")]
        public string NewEmail { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Confirm New Email")]
        [Compare("NewEmail", ErrorMessage = "Email addresses do not match.")]
        public string ConfirmNewEmail { get; set; } = string.Empty;
    }


    public class DoctorAccessViewModel
    {
        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime GrantedDate { get; set; }
        public bool HasAccess { get; set; }
        public List<int> SharedTestRequestIds { get; set; } = new();
    }


    public class DoctorUserViewModel
    {
        public int? Id { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required, StringLength(20)]
        [Display(Name = "HPCSA Number")]
        public string HPCSANumber { get; set; } = string.Empty;

        [Required, EmailAddress]
        [Display(Name = "Email Address (Username)")]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Contact Number")]
        public string ContactNumber { get; set; } = string.Empty;
    }


    public class EditOrderViewModel
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public OrderStatus OrderStatus { get; set; }

        // List of items currently on the order
        public List<OrderItemEditModel> Items { get; set; } = new List<OrderItemEditModel>();

        // For adding new items
        public int? NewConsumableId { get; set; }
        public int? NewQuantity { get; set; }
    }

    public class EditPatientViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required, StringLength(13)]
        [Display(Name = "South African ID Number")]
        public string SouthAfricanIdNumber { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }

        [Required, StringLength(20)]
        [Phone]
        [Display(Name = "Cellphone Number")]
        public string CellphoneNumber { get; set; } = string.Empty;

        [Required, EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Home Address")]
        public string HomeAddress { get; set; } = string.Empty;

        // Medical history - comma-separated strings for simplicity
        [Display(Name = "Medical Conditions (comma separated)")]
        public string MedicalConditionsInput { get; set; } = string.Empty;

        [Display(Name = "Allergies (comma separated)")]
        public string AllergiesInput { get; set; } = string.Empty;

        [Display(Name = "Current Medications (comma separated)")]
        public string MedicationsInput { get; set; } = string.Empty;
    }

    public class EditProfileViewModel
    {
        [Required, StringLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Contact Number")]
        public string? ContactNumber { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email address")]
        public string Email { get; set; } = string.Empty;
    }

    public class GrantAccessViewModel
    {
        public int DoctorId { get; set; }
        public List<int> SelectedTestRequestIds { get; set; } = new();
    }

    public class ImportDataViewModel
    {
        [Required(ErrorMessage = "Please select a file.")]
        public IFormFile File { get; set; }
    }

    public class LabTechnicianViewModel
    {
        public int? Id { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required, StringLength(13)]
        [Display(Name = "South African ID Number")]
        public string SAIDNumber { get; set; } = string.Empty;

        public Status IsActive { get; set; } = Status.Active;


        [Required]
        [Display(Name = "Employee Number")]
        public string EmployeeNumber { get; set; } = string.Empty;

        [Required, EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Contact Number")]
        public string ContactNumber { get; set; } = string.Empty;

        [Display(Name = "Assigned Test Types")]
        public List<int> SelectedTestTypeIds { get; set; } = new List<int>();
    }

    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    public class ManageTwoFactorViewModel
    {
        public bool IsTwoFactorEnabled { get; set; }
        public int RecoveryCodesLeft { get; set; }
    }

    public class MedicalConditionViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Condition Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Category")]
        public string? Category { get; set; }
    }

    public class MedicationViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Medication Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Category")]
        public string? Category { get; set; }
    }

    public class OrderCreateViewModel
    {
        public int SupplierId { get; set; }
        public Dictionary<int, int> ItemQuantities { get; set; } = new(); // ConsumableId -> Quantity
    }

    public class OrderItemEditModel
    {
        public int OrderItemId { get; set; }
        public int ConsumableId { get; set; }
        public string ConsumableName { get; set; } = string.Empty;
        public int QuantityOrdered { get; set; }
        public OrderItemStatus Status { get; set; }
        public bool Remove { get; set; } // Flag for removal in POST
    }

    public class PatientDetailsViewModel
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string SouthAfricanIdNumber { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string CellphoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string HomeAddress { get; set; } = string.Empty;

        // Medical history
        public List<string> MedicalConditions { get; set; } = new();
        public List<string> Allergies { get; set; } = new();
        public List<string> Medications { get; set; } = new();
    }

    public class PatientMedicalHistoryViewModel
    {
        public int PatientId { get; set; }
        public string MedicalConditionsInput { get; set; } = string.Empty;
        public string AllergiesInput { get; set; } = string.Empty;
        public string MedicationsInput { get; set; } = string.Empty;
    }

    public class PatientRegistrationViewModel
    {
        [Required, StringLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required, StringLength(13)]
        [Display(Name = "South African ID Number")]
        public string SouthAfricanIdNumber { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }

        [Required, StringLength(20)]
        [Phone]
        [Display(Name = "Cellphone Number")]
        public string CellphoneNumber { get; set; } = string.Empty;

        [Required, EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Home Address")]
        public string HomeAddress { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[^\w\s]).{8,}$",
            ErrorMessage = "Password must be at least 8 characters and contain at least one uppercase letter, one number, and one special character.")]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }


    public class PatientReportFilterViewModel
    {
        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today.AddMonths(-1);

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.Today;
    }

    public class PatientTrackingViewModel
    {
        public int? SelectedTestTypeId { get; set; }
        public SelectList TestTypeOptions { get; set; } = null!;
        public string? TestName { get; set; }
        public string? Units { get; set; }
        public decimal? NormalMin { get; set; }
        public decimal? NormalMax { get; set; }
        public List<TrackingDataPoint> DataPoints { get; set; } = new();


    }

    public class ReportDateRangeViewModel
    {
        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }


    }

    public class SampleEntryViewModel
    {
        [Required]
        public string Barcode { get; set; } = string.Empty;

        [Required]
        public int SampleTypeId { get; set; }
    }
    public class SampleItemViewModel
    {
        public string Barcode { get; set; } = string.Empty;
        public string SampleType { get; set; } = string.Empty;
        public DateTime? CollectedDate { get; set; }
    }

    public class StockAdjustmentViewModel
    {
        public int ConsumableId { get; set; }
        public string AdjustmentType { get; set; } = "Increase"; // "Increase", "Decrease", "Set"
        public int Quantity { get; set; }
    }

    public class TestTypeItemForProcessingViewModel
    {
        public int TestRequestId { get; set; }
        public int TestTypeId { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string SampleType { get; set; } = string.Empty;
        public RequestStatus Status { get; set; }
        public int? TechnicianId { get; set; }
        public string? TechnicianName { get; set; }
        public DateTime? StartDateTime { get; set; }
        public DateTime? CompletionDateTime { get; set; }
        public bool CanStart => Status == RequestStatus.Submitted;
        public bool CanComplete { get; set; }

        // Verification properties
        public int? VerifiedById { get; set; }
        public string? VerifiedByName { get; set; }
        public DateTime? VerifiedDateTime { get; set; }
        public string? VerificationNotes { get; set; }
        public bool CanVerify { get; set; }
        public bool CanReturnForReview { get; set; }
        public bool CanResubmit { get; set; }

        // ✅ Turnaround / Overdue
        public int TurnaroundTimeMinutes { get; set; }
        public DateTime? ExpectedCompletionTime { get; set; }
        public bool IsOverdue { get; set; }


    }

    public class TrackingDataPoint
    {
        public DateTime? Date { get; set; }
        public string? Value { get; set; }
        public bool IsAbnormal { get; set; }
    }


    public class TwoFactorSetupViewModel
    {
        public string SecretKey { get; set; } = string.Empty;
        public string QrCodeBase64 { get; set; } = string.Empty;  // PNG as base64

        [Required]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be 6 digits.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must be 6 digits.")]
        [Display(Name = "Verification code")]
        public string VerificationCode { get; set; } = string.Empty;
    }

    public class TwoFactorRecoveryCodesViewModel
    {
        public List<string> PlainCodes { get; set; } = new();
    }

    public class TwoFactorChallengeViewModel
    {
        [Display(Name = "Authentication code")]
        public string? Code { get; set; }

        [Display(Name = "Recovery code")]
        public string? RecoveryCode { get; set; }

        public bool UseRecoveryCode { get; set; } = false;

        // Passed through the challenge — needed to complete sign-in
        public string ReturnUrl { get; set; } = string.Empty;

        [Display(Name = "Remember this device (skip 2FA next time)")]
        public bool TrustDevice { get; set; }
    }
    public class TestTypeViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Test Name")]
        public string TestName { get; set; } = string.Empty;

        [Display(Name = "Category")]
        public int TestCategoryId { get; set; }

        [Display(Name = "Sample Type")]
        public int SampleTypeId { get; set; }

        [StringLength(50)]
        [Display(Name = "Units of Measurement")]
        public string? UnitsOfMeasurement { get; set; }

        [Display(Name = "Normal Range Min")]
        public decimal? NormalRangeMin { get; set; }

        [Display(Name = "Normal Range Max")]
        public decimal? NormalRangeMax { get; set; }

        [Display(Name = "Turnaround Time (minutes)")]
        public int TurnaroundTimeMinutes { get; set; }

        [Display(Name = "Consumables Used")]
        public List<int> SelectedConsumableIds { get; set; } = new List<int>();
    }





    public class TestTypeItemViewModel
    {
        public string TestName { get; set; } = string.Empty;
        public string SampleType { get; set; } = string.Empty;
        public RequestStatus Status { get; set; }

        public string? ResultValue { get; set; }
        public bool IsAbnormal { get; set; }
        public string? Notes { get; set; }

        public string? NormalRange { get; set; }
    }

    public class TestCategoryViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Category Name")]
        public string CategoryName { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }
    }

    public class TestResultViewModel
    {
        public int TestRequestTestTypeId { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string? ResultValue { get; set; }
        public string? Units { get; set; }
        public string? NormalRange { get; set; }
        public bool IsAbnormal { get; set; }
        public string? Notes { get; set; }
        public DateTime? CompletedDate { get; set; }
    }



    public class TestRequestListViewModel
    {
        public int Id { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public Urgency Urgency { get; set; }
        public RequestStatus Status { get; set; }
        public int TestCount { get; set; }
    }



    public class TestRequestDetailsViewModel
    {
        public int Id { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public Urgency Urgency { get; set; }
        public string? ClinicalNotes { get; set; }
        public RequestStatus Status { get; set; }

        public DateTime? DateCancelled { get; set; }
        public string? CancellationReason { get; set; }

        public List<TestTypeItemViewModel> TestTypes { get; set; } = new();
        public List<SampleItemViewModel> Samples { get; set; } = new();
    }

    public class TechnicianReportFilterViewModel
    {
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Today.AddMonths(-1);

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; } = DateTime.Today;
    }

    public class TechnicianDashboardViewModel
    {
        // Counts
        public int SelectedTestsCount { get; set; }
        public int WaitingForSelectionCount { get; set; }
        public int WaitingForVerificationCount { get; set; }
        public int WaitingForReviewCount { get; set; }
        public int UrgentTestsCount { get; set; }
        public int OverdueTestsCount { get; set; }
        public int NearingLimitCount { get; set; }

        // Detailed lists
        public List<DashboardTestItemViewModel> SelectedTests { get; set; } = new();
        public List<DashboardTestItemViewModel> WaitingForSelectionTests { get; set; } = new();
        public List<DashboardTestItemViewModel> WaitingForVerificationTests { get; set; } = new();
        public List<DashboardTestItemViewModel> WaitingForReviewTests { get; set; } = new();
        public List<DashboardTestItemViewModel> UrgentTests { get; set; } = new();
        public List<DashboardTestItemViewModel> OverdueTests { get; set; } = new();
        public List<DashboardTestItemViewModel> NearingLimitTests { get; set; } = new();

        // Filters
        public string? FilterUrgency { get; set; }
        public int? FilterCategoryId { get; set; }
        public string? FilterDueTime { get; set; } // "Today", "ThisWeek", "Overdue", etc.
        public string? FilterRequestNumber { get; set; }

        // Dropdown data
        public SelectList UrgencyOptions { get; set; } = new(Enum.GetValues<Urgency>().Select(u => new { Value = u.ToString(), Text = u.ToString() }), "Value", "Text");
        public SelectList CategoryOptions { get; set; } = null!;
        public SelectList DueTimeOptions { get; set; } = new(new[]
        {
            new { Value = "", Text = "All" },
            new { Value = "Today", Text = "Due Today" },
            new { Value = "ThisWeek", Text = "Due This Week" },
            new { Value = "Overdue", Text = "Overdue" },
            new { Value = "Nearing", Text = "Nearing Limit (within 30 min)" }
        }, "Value", "Text");
    }

    public class SupplierViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Supplier Name")]
        public string SupplierName { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Contact Person")]
        public string? ContactPerson { get; set; }

        [EmailAddress]
        [Display(Name = "Email Address")]
        public string? EmailAddress { get; set; }
    }




    public class SampleItemToReceiveViewModel
    {
        public int SampleId { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string SampleType { get; set; } = string.Empty;
        public bool IsReceived { get; set; }
        public DateTime? ReceivedDate { get; set; }
    }




    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[^\w\s]).{8,}$",
            ErrorMessage = "Password must be at least 8 characters and contain at least one uppercase letter, one number, and one special character.")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ReleaseResultsViewModel
    {
        public int RequestId { get; set; }

        [Required(ErrorMessage = "Please enter a note for the patient.")]
        [Display(Name = "Note to Patient")]
        public string Note { get; set; } = string.Empty;

        [Display(Name = "Attach PDF of results")]
        public bool AttachPdf { get; set; } = true;

        [Display(Name = "Ask patient to schedule appointment")]
        public bool RequestAppointment { get; set; }
    }



    public class ProcessTestRequestListViewModel
    {
        public int Id { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public Urgency Urgency { get; set; }
        public RequestStatus Status { get; set; }
        public int TotalTests { get; set; }
        public int CompletedTests { get; set; }
    }


    public class ReceiveSampleViewModel
    {
        public int TestRequestId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public List<SampleItemToReceiveViewModel> Samples { get; set; } = new();
    }






    public class PdfAccessRequestViewModel
    {
        public int Id { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public int TestRequestId { get; set; }
        public Urgency Urgency { get; set; }
    }

    public class PendingTestRequestViewModel
    {
        public int Id { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public Urgency Urgency { get; set; }
        public int SampleCount { get; set; }
    }



    public class PatientTestResultItemViewModel
    {
        public string TestName { get; set; } = string.Empty;
        public string? ResultValue { get; set; }
        public string? Units { get; set; }
        public string? NormalRange { get; set; }
        public bool IsAbnormal { get; set; }
        public string? Notes { get; set; }
        public DateTime? CompletedDate { get; set; }
    }


    public class PatientTestRequestDetailsViewModel
    {
        public int Id { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public Urgency Urgency { get; set; }
        public string? ClinicalNotes { get; set; }
        public RequestStatus Status { get; set; }
        public bool CanViewResults { get; set; }
        public List<PatientTestResultItemViewModel> TestResults { get; set; } = new();

        public bool HasGrantedAccess { get; set; }
    }


    public class PatientTestRequestListViewModel
    {
        public int Id { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public Urgency Urgency { get; set; }
        public RequestStatus Status { get; set; }
        public int TestCount { get; set; }
        public bool HasResults => Status == RequestStatus.Completed || Status == RequestStatus.ReleasedByDoctor;

    }






    public class PatientProfileViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required, StringLength(13)]
        [Display(Name = "South African ID Number")]
        public string SouthAfricanIdNumber { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }

        [Required, StringLength(20)]
        [Phone]
        [Display(Name = "Cellphone Number")]
        public string CellphoneNumber { get; set; } = string.Empty;

        [Required, EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Home Address")]
        public string HomeAddress { get; set; } = string.Empty;
    }



    public class PatientListViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SouthAfricanIdNumber { get; set; } = string.Empty;
        public string CellphoneNumber { get; set; } = string.Empty;
        public Status IsActive { get; set; }

        public int? RegisteredByDoctorId { get; set; }
        public string RegisteredByDoctorName { get; set; } = "Self";
    }





    public class EditTestRequestViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Patient")]
        public int PatientId { get; set; }

        [Display(Name = "Urgency")]
        public Urgency Urgency { get; set; } = Urgency.Routine;

        [Display(Name = "Clinical Notes")]
        public string? ClinicalNotes { get; set; }

        [Required(ErrorMessage = "Please select at least one test type.")]
        [Display(Name = "Test Types")]
        public List<int> SelectedTestTypeIds { get; set; } = new();

        // Samples – only editable if status allows
        public List<SampleEntryViewModel> Samples { get; set; } = new();
        public bool CanEditSamples { get; set; } = true;
    }


    public class DoctorReportFilterViewModel
    {
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Today.AddMonths(-1);

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; } = DateTime.Today;
    }

    public class DashboardTestItemViewModel
    {
        public int TestRequestId { get; set; }
        public int TestTypeId { get; set; }
        public string RequestNumber => $"REQ-{TestRequestId:D6}";
        public string PatientName { get; set; } = string.Empty;
        public string TestName { get; set; } = string.Empty;
        public Urgency Urgency { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public DateTime? StartDateTime { get; set; }
        public DateTime? ExpectedCompletionTime { get; set; }
        public bool IsOverdue { get; set; }
        public bool IsNearingLimit { get; set; }
        public string Status { get; set; } = string.Empty;
    }



    public class ConsumableViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Consumable Name")]
        public string ConsumableName { get; set; } = string.Empty;

        [Display(Name = "Reorder Level")]
        public int ReorderLevel { get; set; }

        [Display(Name = "Quantity On Hand")]
        public int QuantityOnHand { get; set; }

        [Display(Name = "Supplier")]
        public int SupplierId { get; set; }
    }




}
