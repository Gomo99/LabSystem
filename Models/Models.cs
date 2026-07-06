using LaboratoryTestRequestManagementSystem.AppStatus;
using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.Models
{

    public class Allergy
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = null!;

        [StringLength(50)]
        public string? Category { get; set; }

        public Status Status { get; set; } = Status.Active;
        public ICollection<PatientAllergy> PatientAllergies { get; set; } = new List<PatientAllergy>();
    }



    public class Consumable
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string ConsumableName { get; set; } = null!;

        public int ReorderLevel { get; set; }
        public int QuantityOnHand { get; set; }

        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;

        public Status Status { get; set; } = Status.Active;
        public ICollection<TestTypeConsumable> TestTypeConsumables { get; set; } = new List<TestTypeConsumable>();
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }


    public class DoctorPatientAccess
    {
        public int Id { get; set; }

        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        public int DoctorId { get; set; }
        public Employee Doctor { get; set; } = null!; // Assuming Employee with Role.Doctor

        public DateTime GrantedDate { get; set; } = DateTime.Now;

        // Optional: track which test requests are shared (can be expanded)
        public string? SharedTestRequestIds { get; set; } // Comma-separated or JSON

        public Status Status { get; set; } = Status.Active;
    }


    public class Employee
    {
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; } = null!;

        [Required]
        public string LastName { get; set; } = null!;

        // Login username (email)
        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        // Legacy field – can be kept but not used for login
        public string? Username { get; set; }

        [Required]
        public string PasswordHash { get; set; } = null!;

        [Phone]
        public string? ContactNumber { get; set; }

        // Doctor specific
        [StringLength(20)]
        public string? HPCSANumber { get; set; }      // Unique for doctors

        // Technician specific
        [StringLength(13)]
        public string? SAIDNumber { get; set; }       // Unique for technicians
        public string? EmployeeNumber { get; set; }

        public UserRole Role { get; set; }
        public Status IsActive { get; set; } = Status.Active;
        public int FailedAttempts { get; set; }
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }
        public bool MustChangePassword { get; set; } = false;
        public string? EmailVerificationTokenHash { get; set; }
        public DateTime? EmailVerificationTokenExpires { get; set; }
        public bool IsTwoFactorEnabled { get; set; } = false;
        public string? TwoFactorSecretKey { get; set; }
        public string? TwoFactorRecoveryCodes { get; set; }
        public string? ResetPin { get; set; }
        public DateTime? ResetPinExpiration { get; set; }
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutEnd { get; set; }

        // Navigation for technician test type assignments
        public ICollection<TechnicianTestType> TechnicianTestTypes { get; set; } = new List<TechnicianTestType>();
    }


    public class ImportedTestResult
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string TestName { get; set; }
        public string ResultValue { get; set; }
        public string Units { get; set; }
        public string NormalRange { get; set; }
        public DateTime? ResultDate { get; set; }
        public string LabName { get; set; }  // original lab

        public Patient Patient { get; set; }
    }


    public class MedicalCondition
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = null!;

        [StringLength(50)]
        public string? Category { get; set; }
        public Status Status { get; set; } = Status.Active;

        public ICollection<PatientCondition> PatientConditions { get; set; } = new List<PatientCondition>();
    }


    public class Medication
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = null!;

        [StringLength(50)]
        public string? Category { get; set; }
        public Status Status { get; set; } = Status.Active;

        public ICollection<PatientMedication> PatientMedications { get; set; } = new List<PatientMedication>();
    }


    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }          // ID of the target user
        public string UserType { get; set; }    // "Doctor", "Patient", "LabTechnician", "LaboratoryManager"
        public string Message { get; set; }
        public string Link { get; set; }        // e.g., "/Doctor/RequestDetails/5"
        public bool IsRead { get; set; } = false;
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public Status Status { get; set; } = Status.Active;
    }


    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string OrderNumber { get; set; } = null!;

        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;

        public DateTime OrderDate { get; set; } = DateTime.Now;

        // Business status (Order lifecycle)
        public OrderStatus OrderStatus { get; set; } = OrderStatus.Ordered;

        public DateTime? DateCompleted { get; set; }
        public DateTime? DateCancelled { get; set; }
        public string? CancellationReason { get; set; }

        // Soft delete / system status (ACTIVE / INACTIVE)
        public Status Status { get; set; } = Status.Active;

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }




    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public int ConsumableId { get; set; }
        public Consumable Consumable { get; set; } = null!;

        public int QuantityOrdered { get; set; }

        // Business workflow status
        public OrderItemStatus OrderItemStatus { get; set; } = OrderItemStatus.Ordered;

        public Status Status { get; set; } = Status.Active;

        public DateTime? DateReceived { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime? DateCancelled { get; set; }
    }





    public class Patient
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string FirstName { get; set; } = null!;

        [Required, StringLength(50)]
        public string LastName { get; set; } = null!;

        [Required, StringLength(13)]
        public string SouthAfricanIdNumber { get; set; } = null!;

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required, StringLength(20)]
        public string CellphoneNumber { get; set; } = null!;

        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public string HomeAddress { get; set; } = null!;

        [Required]
        public string PasswordHash { get; set; } = null!;

        public Status IsActive { get; set; } = Status.Active;

        public bool MustChangePassword { get; set; } = false;
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }
        public Status Status { get; set; } = Status.Active;

        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutEnd { get; set; }

        // NEW – who registered the patient (null = self‑registered)
        public int? RegisteredByDoctorId { get; set; }
        public Employee? RegisteredByDoctor { get; set; }

        // Navigation properties
        public ICollection<PatientCondition> PatientConditions { get; set; } = new List<PatientCondition>();
        public ICollection<PatientAllergy> PatientAllergies { get; set; } = new List<PatientAllergy>();
        public ICollection<PatientMedication> PatientMedications { get; set; } = new List<PatientMedication>();
        public ICollection<DoctorPatientAccess> DoctorAccessGrants { get; set; } = new List<DoctorPatientAccess>();
    }



    public class PatientAllergy
    {
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        public int AllergyId { get; set; }
        public Allergy Allergy { get; set; } = null!;

        public Status Status { get; set; } = Status.Active;

    }



    public class PatientCondition
    {
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        public int MedicalConditionId { get; set; }
        public MedicalCondition MedicalCondition { get; set; } = null!;

        public Status Status { get; set; } = Status.Active;


    }


    public class PatientMedication
    {
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        public int MedicationId { get; set; }
        public Medication Medication { get; set; } = null!;

        public Status Status { get; set; } = Status.Active;

    }

    public class ReportAccessRequest
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public int TestRequestId { get; set; }          // The specific test request
        public DateTime RequestDate { get; set; } = DateTime.Now;
        public AccessRequestStatus Status { get; set; } = AccessRequestStatus.Pending;
        public DateTime? ResponseDate { get; set; }
        public string? DenyReason { get; set; }

        // Navigation
        public Patient Patient { get; set; } = null!;
        public Employee Doctor { get; set; } = null!;
        public TestRequest TestRequest { get; set; } = null!;
    }


    public class Sample
    {
        public int Id { get; set; }

        [Required]
        public string Barcode { get; set; } = null!; // Unique

        public int TestRequestId { get; set; }
        public TestRequest TestRequest { get; set; } = null!;

        public int SampleTypeId { get; set; }
        public SampleType SampleType { get; set; } = null!;

        public DateTime? CollectedDate { get; set; }
        public DateTime? ReceivedDate { get; set; }

        // ✅ Technician who received the sample
        public int? ReceivedById { get; set; }
        public Employee? ReceivedBy { get; set; }

        public Status Status { get; set; } = Status.Active;
    }



    public class SampleType
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string Name { get; set; } = null!;

        public Status Status { get; set; } = Status.Active;


    }



    public class Supplier
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string SupplierName { get; set; } = null!;

        [StringLength(100)]
        public string? ContactPerson { get; set; }

        [EmailAddress]
        public string? EmailAddress { get; set; }
        public Status Status { get; set; } = Status.Active;
        public ICollection<Consumable> Consumables { get; set; } = new List<Consumable>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }

    public class TechnicianTestType
    {
        public int TechnicianId { get; set; }
        public Employee Technician { get; set; } = null!; // Role = LabTechnician

        public int TestTypeId { get; set; }
        public TestType TestType { get; set; } = null!;

        public Status Status { get; set; } = Status.Active;
    }

    public class TestCategory
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string CategoryName { get; set; } = null!;

        public string? Description { get; set; }

        public ICollection<TestType> TestTypes { get; set; } = new List<TestType>();

        public Status Status { get; set; } = Status.Active;
    }




    public class TestRequest
    {
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        [Required]
        public int DoctorId { get; set; }
        public Employee Doctor { get; set; } = null!;

        public DateTime RequestDate { get; set; } = DateTime.Now;

        public Urgency Urgency { get; set; } = Urgency.Routine;

        public string? ClinicalNotes { get; set; }

        public RequestStatus RequestStatus { get; set; } = RequestStatus.Submitted;

        public Status RecordStatus { get; set; } = Status.Active; // Soft delete

        // ✅ Cancellation fields
        public DateTime? DateCancelled { get; set; }
        public string? CancellationReason { get; set; }

        // Navigation properties
        public ICollection<TestRequestTestType> TestRequestTestTypes { get; set; } = new List<TestRequestTestType>();
        public ICollection<Sample> Samples { get; set; } = new List<Sample>();
    }




    public class TestRequestTestType
    {
        public int TestRequestId { get; set; }
        public TestRequest TestRequest { get; set; } = null!;

        public int TestTypeId { get; set; }
        public TestType TestType { get; set; } = null!;

        public RequestStatus RequestStatus { get; set; } = RequestStatus.Submitted;
        public Status RecordStatus { get; set; } = Status.Active;

        // Processing fields
        public int? TechnicianId { get; set; }
        public Employee? Technician { get; set; }
        public DateTime? StartDateTime { get; set; }
        public DateTime? CompletionDateTime { get; set; }

        // ✅ Verification fields
        public int? VerifiedById { get; set; }
        public Employee? VerifiedBy { get; set; }
        public DateTime? VerifiedDateTime { get; set; }
        public string? VerificationNotes { get; set; }
        public string? ReviewNotes { get; set; } // Notes from original technician when resubmitting
    }



    public class TestResult
    {
        public int Id { get; set; }

        [Required]
        public int TestRequestId { get; set; }
        public TestRequest TestRequest { get; set; } = null!;

        [Required]
        public int TestTypeId { get; set; }
        public TestType TestType { get; set; } = null!;

        public string? ResultValue { get; set; }
        public string? Notes { get; set; }

        public bool IsAbnormal { get; set; }

        public DateTime? CompletedDate { get; set; }
        public DateTime? VerifiedDate { get; set; }

        public int? VerifiedById { get; set; }
        public Employee? VerifiedBy { get; set; }

        public Status Status { get; set; } = Status.Active;
    }



    public class TestReviewHistory
    {
        public int Id { get; set; }

        public int TestRequestId { get; set; }
        public TestRequest TestRequest { get; set; } = null!;

        public int TestTypeId { get; set; }
        public TestType TestType { get; set; } = null!;

        public int? ReviewerId { get; set; }
        public Employee? Reviewer { get; set; }

        public DateTime ReviewDate { get; set; } = DateTime.Now;

        [Required]
        public string Action { get; set; } = string.Empty; // "Verified", "Returned", "Resubmitted"

        public string? Notes { get; set; }
    }




    public class TestType
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string TestName { get; set; } = null!;

        public int TestCategoryId { get; set; }
        public TestCategory TestCategory { get; set; } = null!;

        public int SampleTypeId { get; set; }
        public SampleType SampleType { get; set; } = null!;

        [StringLength(50)]
        public string? UnitsOfMeasurement { get; set; }

        public decimal? NormalRangeMin { get; set; }
        public decimal? NormalRangeMax { get; set; }
        public Status Status { get; set; } = Status.Active;
        public int TurnaroundTimeMinutes { get; set; }

        // Many-to-many with Consumable
        public ICollection<TestTypeConsumable> TestTypeConsumables { get; set; } = new List<TestTypeConsumable>();

        // Many-to-many with LabTechnician (for assignment)
        public ICollection<TechnicianTestType> TechnicianTestTypes { get; set; } = new List<TechnicianTestType>();
    }



    public class TestTypeConsumable
    {
        public int TestTypeId { get; set; }
        public TestType TestType { get; set; } = null!;

        public int ConsumableId { get; set; }
        public Consumable Consumable { get; set; } = null!;

        public Status Status { get; set; } = Status.Active;

    }



    public class UserDevice
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }        // Employee.Id or Patient.Id

        [Required]
        public string UserType { get; set; } = null!; // "Employee" or "Patient"

        [Required]
        public string DeviceId { get; set; } = null!; // Unique identifier stored in cookie

        [Required]
        public string DeviceName { get; set; } = null!; // User-agent or custom name

        public DateTime FirstSeen { get; set; } = DateTime.Now;
        public DateTime LastSeen { get; set; } = DateTime.Now;
        public bool IsTrusted { get; set; } = false; // For 2FA bypass

        [StringLength(45)]
        public string? IpAddress { get; set; }
    }


}
