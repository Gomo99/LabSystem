using LaboratoryTestRequestManagementSystem.AppStatus;
using LaboratoryTestRequestManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LaboratoryTestRequestManagementSystem.Data
{
    public class LabDbContext : DbContext
    {
        public LabDbContext(DbContextOptions options) : base(options) { }

        // Core user tables
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<UserDevice> UserDevices { get; set; }

        // Medical history lookup tables
        public DbSet<MedicalCondition> MedicalConditions { get; set; }
        public DbSet<Allergy> Allergies { get; set; }
        public DbSet<Medication> Medications { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        // Patient medical history junction tables
        public DbSet<PatientCondition> PatientConditions { get; set; }
        public DbSet<PatientAllergy> PatientAllergies { get; set; }
        public DbSet<PatientMedication> PatientMedications { get; set; }

        // Consent / access management
        public DbSet<DoctorPatientAccess> DoctorPatientAccesses { get; set; }

        // Laboratory Manager subsystem
        public DbSet<TestCategory> TestCategories { get; set; }
        public DbSet<TestType> TestTypes { get; set; }
        public DbSet<SampleType> SampleTypes { get; set; }
        public DbSet<Consumable> Consumables { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        // Junction tables
        public DbSet<TestTypeConsumable> TestTypeConsumables { get; set; }
        public DbSet<TechnicianTestType> TechnicianTestTypes { get; set; }

        // Test request workflow
        public DbSet<TestRequest> TestRequests { get; set; }
        public DbSet<TestRequestTestType> TestRequestTestTypes { get; set; }
        public DbSet<Sample> Samples { get; set; }
        public DbSet<TestResult> TestResults { get; set; }
        public DbSet<TestReviewHistory> TestReviewHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ------------------------------------------------------------
            // 1. Global Enum → String and Bool → String Conversion
            // ------------------------------------------------------------
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    var clrType = property.ClrType;

                    // Convert all enums to strings
                    if (clrType.IsEnum)
                    {
                        var converterType = typeof(EnumToStringConverter<>).MakeGenericType(clrType);
                        var converter = (ValueConverter)Activator.CreateInstance(converterType)!;
                        property.SetValueConverter(converter);
                    }
                    // Convert all booleans to strings ("True"/"False")
                    else if (clrType == typeof(bool) || clrType == typeof(bool?))
                    {
                        property.SetValueConverter(new ValueConverter<bool, string>(
                            v => v.ToString(),
                            v => bool.Parse(v)));
                        property.SetMaxLength(5); // "False" is longest
                    }
                }
            }

            // ------------------------------------------------------------
            // 2. Composite Primary Keys (Junction Tables)
            // ------------------------------------------------------------
            modelBuilder.Entity<PatientCondition>()
                .HasKey(pc => new { pc.PatientId, pc.MedicalConditionId });

            modelBuilder.Entity<PatientAllergy>()
                .HasKey(pa => new { pa.PatientId, pa.AllergyId });

            modelBuilder.Entity<PatientMedication>()
                .HasKey(pm => new { pm.PatientId, pm.MedicationId });

            modelBuilder.Entity<TestTypeConsumable>()
                .HasKey(tc => new { tc.TestTypeId, tc.ConsumableId });

            modelBuilder.Entity<TechnicianTestType>()
                .HasKey(tt => new { tt.TechnicianId, tt.TestTypeId });

            modelBuilder.Entity<TestRequestTestType>()
                .HasKey(trt => new { trt.TestRequestId, trt.TestTypeId });

            // ------------------------------------------------------------
            // 3. Unique Constraints
            // ------------------------------------------------------------
            modelBuilder.Entity<Patient>()
                .HasIndex(p => p.SouthAfricanIdNumber)
                .IsUnique();

            modelBuilder.Entity<Patient>()
                .HasIndex(p => p.Email)
                .IsUnique();

            modelBuilder.Entity<Supplier>()
                .HasIndex(s => s.SupplierName)
                .IsUnique();

            modelBuilder.Entity<TestCategory>()
                .HasIndex(tc => tc.CategoryName)
                .IsUnique();

            modelBuilder.Entity<Consumable>()
                .HasIndex(c => c.ConsumableName)
                .IsUnique();

            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.Email)
                .IsUnique();

            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.HPCSANumber)
                .IsUnique()
                .HasFilter("[HPCSANumber] IS NOT NULL AND [Role] = 'Doctor'");

            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.SAIDNumber)
                .IsUnique()
                .HasFilter("[SAIDNumber] IS NOT NULL AND [Role] = 'LabTechnician'");

            modelBuilder.Entity<Sample>()
                .HasIndex(s => s.Barcode)
                .IsUnique();

            modelBuilder.Entity<UserDevice>()
     .HasIndex(ud => new { ud.DeviceId, ud.UserId, ud.UserType })
     .IsUnique();

            // ------------------------------------------------------------
            // 4. Decimal Precision Configuration (suppress warnings)
            // ------------------------------------------------------------
            modelBuilder.Entity<TestType>()
                .Property(tt => tt.NormalRangeMin)
                .HasPrecision(18, 4);

            modelBuilder.Entity<TestType>()
                .Property(tt => tt.NormalRangeMax)
                .HasPrecision(18, 4);

            // ------------------------------------------------------------
            // 5. Foreign Key Relationships (Explicit for clarity)
            // ------------------------------------------------------------
            // Employee → TechnicianTestType (One-to-Many)
            modelBuilder.Entity<TechnicianTestType>()
                .HasOne(tt => tt.Technician)
                .WithMany(e => e.TechnicianTestTypes)
                .HasForeignKey(tt => tt.TechnicianId)
                .OnDelete(DeleteBehavior.Restrict);

            // Employee → TestRequestTestType (as Technician)
            modelBuilder.Entity<TestRequestTestType>()
                .HasOne(trt => trt.Technician)
                .WithMany()
                .HasForeignKey(trt => trt.TechnicianId)
                .OnDelete(DeleteBehavior.Restrict);

            // Employee → TestRequestTestType (as Verifier)
            modelBuilder.Entity<TestRequestTestType>()
                .HasOne(trt => trt.VerifiedBy)
                .WithMany()
                .HasForeignKey(trt => trt.VerifiedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Employee → TestRequest (as Doctor)
            modelBuilder.Entity<TestRequest>()
                .HasOne(tr => tr.Doctor)
                .WithMany()
                .HasForeignKey(tr => tr.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Employee → DoctorPatientAccess (as Doctor)
            modelBuilder.Entity<DoctorPatientAccess>()
                .HasOne(dpa => dpa.Doctor)
                .WithMany()
                .HasForeignKey(dpa => dpa.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Employee → TestReviewHistory (as Reviewer)
            modelBuilder.Entity<TestReviewHistory>()
                .HasOne(trh => trh.Reviewer)
                .WithMany()
                .HasForeignKey(trh => trh.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Employee → TestResult (as Verifier)
            modelBuilder.Entity<TestResult>()
                .HasOne(tr => tr.VerifiedBy)
                .WithMany()
                .HasForeignKey(tr => tr.VerifiedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Employee → Sample (as Receiver)
            modelBuilder.Entity<Sample>()
                .HasOne(s => s.ReceivedBy)
                .WithMany()
                .HasForeignKey(s => s.ReceivedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Patient → PatientCondition
            modelBuilder.Entity<PatientCondition>()
                .HasOne(pc => pc.Patient)
                .WithMany(p => p.PatientConditions)
                .HasForeignKey(pc => pc.PatientId);

            // Patient → PatientAllergy
            modelBuilder.Entity<PatientAllergy>()
                .HasOne(pa => pa.Patient)
                .WithMany(p => p.PatientAllergies)
                .HasForeignKey(pa => pa.PatientId);

            // Patient → PatientMedication
            modelBuilder.Entity<PatientMedication>()
                .HasOne(pm => pm.Patient)
                .WithMany(p => p.PatientMedications)
                .HasForeignKey(pm => pm.PatientId);

            // Patient → DoctorPatientAccess
            modelBuilder.Entity<DoctorPatientAccess>()
                .HasOne(dpa => dpa.Patient)
                .WithMany(p => p.DoctorAccessGrants)
                .HasForeignKey(dpa => dpa.PatientId);

            // Patient → TestRequest
            modelBuilder.Entity<TestRequest>()
                .HasOne(tr => tr.Patient)
                .WithMany()
                .HasForeignKey(tr => tr.PatientId);

            // MedicalCondition → PatientCondition
            modelBuilder.Entity<PatientCondition>()
                .HasOne(pc => pc.MedicalCondition)
                .WithMany(mc => mc.PatientConditions)
                .HasForeignKey(pc => pc.MedicalConditionId);

            // Allergy → PatientAllergy
            modelBuilder.Entity<PatientAllergy>()
                .HasOne(pa => pa.Allergy)
                .WithMany(a => a.PatientAllergies)
                .HasForeignKey(pa => pa.AllergyId);

            // Medication → PatientMedication
            modelBuilder.Entity<PatientMedication>()
                .HasOne(pm => pm.Medication)
                .WithMany(m => m.PatientMedications)
                .HasForeignKey(pm => pm.MedicationId);

            // TestCategory → TestType
            modelBuilder.Entity<TestType>()
                .HasOne(tt => tt.TestCategory)
                .WithMany(tc => tc.TestTypes)
                .HasForeignKey(tt => tt.TestCategoryId);

            // SampleType → TestType
            modelBuilder.Entity<TestType>()
                .HasOne(tt => tt.SampleType)
                .WithMany()
                .HasForeignKey(tt => tt.SampleTypeId);

            // SampleType → Sample
            modelBuilder.Entity<Sample>()
                .HasOne(s => s.SampleType)
                .WithMany()
                .HasForeignKey(s => s.SampleTypeId);

            // TestType → TestTypeConsumable
            modelBuilder.Entity<TestTypeConsumable>()
                .HasOne(ttc => ttc.TestType)
                .WithMany(tt => tt.TestTypeConsumables)
                .HasForeignKey(ttc => ttc.TestTypeId);

            // Consumable → TestTypeConsumable
            modelBuilder.Entity<TestTypeConsumable>()
                .HasOne(ttc => ttc.Consumable)
                .WithMany(c => c.TestTypeConsumables)
                .HasForeignKey(ttc => ttc.ConsumableId);

            // TestType → TechnicianTestType
            modelBuilder.Entity<TechnicianTestType>()
                .HasOne(ttt => ttt.TestType)
                .WithMany(tt => tt.TechnicianTestTypes)
                .HasForeignKey(ttt => ttt.TestTypeId);

            // TestType → TestRequestTestType
            modelBuilder.Entity<TestRequestTestType>()
                .HasOne(trt => trt.TestType)
                .WithMany()
                .HasForeignKey(trt => trt.TestTypeId);

            // TestType → TestResult
            modelBuilder.Entity<TestResult>()
                .HasOne(tr => tr.TestType)
                .WithMany()
                .HasForeignKey(tr => tr.TestTypeId);

            // TestType → TestReviewHistory
            modelBuilder.Entity<TestReviewHistory>()
                .HasOne(trh => trh.TestType)
                .WithMany()
                .HasForeignKey(trh => trh.TestTypeId);

            // Consumable → OrderItem
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Consumable)
                .WithMany(c => c.OrderItems)
                .HasForeignKey(oi => oi.ConsumableId);

            // Supplier → Consumable
            modelBuilder.Entity<Consumable>()
                .HasOne(c => c.Supplier)
                .WithMany(s => s.Consumables)
                .HasForeignKey(c => c.SupplierId);

            // Supplier → Order
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Supplier)
                .WithMany(s => s.Orders)
                .HasForeignKey(o => o.SupplierId);

            // ✅ Order → OrderItem – changed to Restrict to avoid multiple cascade paths
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            // TestRequest → TestRequestTestType
            modelBuilder.Entity<TestRequestTestType>()
                .HasOne(trt => trt.TestRequest)
                .WithMany(tr => tr.TestRequestTestTypes)
                .HasForeignKey(trt => trt.TestRequestId);

            // TestRequest → Sample
            modelBuilder.Entity<Sample>()
                .HasOne(s => s.TestRequest)
                .WithMany(tr => tr.Samples)
                .HasForeignKey(s => s.TestRequestId);

            // TestRequest → TestResult
            modelBuilder.Entity<TestResult>()
                .HasOne(tr => tr.TestRequest)
                .WithMany()
                .HasForeignKey(tr => tr.TestRequestId);

            // TestRequest → TestReviewHistory
            modelBuilder.Entity<TestReviewHistory>()
                .HasOne(trh => trh.TestRequest)
                .WithMany()
                .HasForeignKey(trh => trh.TestRequestId);


            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("Notifications");
                entity.HasKey(n => n.Id);
                entity.Property(n => n.Message).IsRequired().HasMaxLength(500);
                entity.Property(n => n.UserType).IsRequired().HasMaxLength(50);
                entity.Property(n => n.Status).HasDefaultValue(Status.Active);
            });

            // ------------------------------------------------------------
            // 6. Default Values for Status / IsActive columns
            // ------------------------------------------------------------
            modelBuilder.Entity<TestCategory>().Property(tc => tc.Status).HasDefaultValue(Status.Active);
            modelBuilder.Entity<TestType>().Property(tt => tt.Status).HasDefaultValue(Status.Active);
            modelBuilder.Entity<Consumable>().Property(c => c.Status).HasDefaultValue(Status.Active);
            modelBuilder.Entity<Supplier>().Property(s => s.Status).HasDefaultValue(Status.Active);
            modelBuilder.Entity<Allergy>().Property(a => a.Status).HasDefaultValue(Status.Active);
            modelBuilder.Entity<PatientAllergy>().Property(pa => pa.Status).HasDefaultValue(Status.Active);
            modelBuilder.Entity<PatientCondition>().Property(pc => pc.Status).HasDefaultValue(Status.Active);
            modelBuilder.Entity<PatientMedication>().Property(pm => pm.Status).HasDefaultValue(Status.Active);
            modelBuilder.Entity<Employee>().Property(e => e.IsActive).HasDefaultValue(Status.Active);
            modelBuilder.Entity<Patient>().Property(p => p.IsActive).HasDefaultValue(Status.Active);
            modelBuilder.Entity<MedicalCondition>().Property(mc => mc.Status).HasDefaultValue(Status.Active);
            modelBuilder.Entity<Medication>().Property(m => m.Status).HasDefaultValue(Status.Active);
            modelBuilder.Entity<TestTypeConsumable>().Property(tc => tc.Status).HasDefaultValue(Status.Active);
            modelBuilder.Entity<TechnicianTestType>().Property(tt => tt.Status).HasDefaultValue(Status.Active);
            modelBuilder.Entity<SampleType>().Property(st => st.Status).HasDefaultValue(Status.Active);
            modelBuilder.Entity<Order>().Property(o => o.Status).HasDefaultValue(Status.Active);
            modelBuilder.Entity<OrderItem>().Property(oi => oi.Status).HasDefaultValue(Status.Active);
            modelBuilder.Entity<TestRequest>().Property(tr => tr.RecordStatus).HasDefaultValue(Status.Active);
            modelBuilder.Entity<TestRequestTestType>().Property(trt => trt.RecordStatus).HasDefaultValue(Status.Active);
            modelBuilder.Entity<Sample>().Property(s => s.Status).HasDefaultValue(Status.Active);
            modelBuilder.Entity<TestResult>().Property(tr => tr.Status).HasDefaultValue(Status.Active);
            modelBuilder.Entity<DoctorPatientAccess>().Property(dpa => dpa.Status).HasDefaultValue(Status.Active);

            // ------------------------------------------------------------
            // 7. SEED DATA – Default Users for Each Role (Plain‑text password)
            // ------------------------------------------------------------
            // Password: "Temp123!"

            modelBuilder.Entity<Employee>().HasData(new Employee
            {
                Id = 1,
                FirstName = "System",
                LastName = "Administrator",
                Email = "admin@nmbhlabsys.co.za",
                Username = "admin@nmbhlabsys.co.za",
                PasswordHash = "Temp123!",
                Role = UserRole.Admin,
                IsActive = Status.Active,
                MustChangePassword = true,
                FailedLoginAttempts = 0
            });

            modelBuilder.Entity<Employee>().HasData(new Employee
            {
                Id = 2,
                FirstName = "Lab",
                LastName = "Manager",
                Email = "labmanager@nmbhlabsys.co.za",
                Username = "labmanager@nmbhlabsys.co.za",
                PasswordHash = "Temp123!",
                Role = UserRole.LaboratoryManager,
                IsActive = Status.Active,
                MustChangePassword = true,
                FailedLoginAttempts = 0
            });

            modelBuilder.Entity<Employee>().HasData(new Employee
            {
                Id = 3,
                FirstName = "John",
                LastName = "Doe",
                Email = "dr.doe@nmbhlabsys.co.za",
                Username = "dr.doe@nmbhlabsys.co.za",
                HPCSANumber = "MP0123456",
                ContactNumber = "0821112222",
                PasswordHash = "Temp123!",
                Role = UserRole.Doctor,
                IsActive = Status.Active,
                MustChangePassword = true,
                FailedLoginAttempts = 0
            });

            modelBuilder.Entity<Employee>().HasData(new Employee
            {
                Id = 4,
                FirstName = "Jane",
                LastName = "Smith",
                Email = "tech.smith@nmbhlabsys.co.za",
                Username = "tech.smith@nmbhlabsys.co.za",
                SAIDNumber = "9001010123087",
                EmployeeNumber = "TECH001",
                ContactNumber = "0832223333",
                PasswordHash = "Temp123!",
                Role = UserRole.LabTechnician,
                IsActive = Status.Active,
                MustChangePassword = true,
                FailedLoginAttempts = 0
            });

            modelBuilder.Entity<Patient>().HasData(new Patient
            {
                Id = 1,
                FirstName = "Alice",
                LastName = "Johnson",
                SouthAfricanIdNumber = "8505050123088",
                DateOfBirth = new DateTime(1985, 5, 5),
                CellphoneNumber = "0843334444",
                Email = "alice.johnson@example.com",
                HomeAddress = "123 Main Street, Gqeberha",
                PasswordHash = "Temp123!",
                IsActive = Status.Active,
                MustChangePassword = true,
                FailedLoginAttempts = 0
            });


            modelBuilder.Entity<Allergy>().HasData(
                new Allergy { Id = 1, Name = "Penicillin", Category = "Antibiotics", Status = Status.Active },
                new Allergy { Id = 2, Name = "Sulfa Drugs", Category = "Antibiotics", Status = Status.Active },
                new Allergy { Id = 3, Name = "Latex", Category = "Environmental", Status = Status.Active },
                new Allergy { Id = 4, Name = "Peanuts", Category = "Food", Status = Status.Active },
                new Allergy { Id = 5, Name = "Shellfish", Category = "Food", Status = Status.Active }
            );

            // Medical Conditions
            modelBuilder.Entity<MedicalCondition>().HasData(
                new MedicalCondition { Id = 1, Name = "Diabetes Mellitus Type 2", Category = "Endocrine", Status = Status.Active },
                new MedicalCondition { Id = 2, Name = "Hypertension", Category = "Cardiovascular", Status = Status.Active },
                new MedicalCondition { Id = 3, Name = "Asthma", Category = "Respiratory", Status = Status.Active },
                new MedicalCondition { Id = 4, Name = "HIV", Category = "Infectious Disease", Status = Status.Active },
                new MedicalCondition { Id = 5, Name = "Tuberculosis", Category = "Infectious Disease", Status = Status.Active },
                new MedicalCondition { Id = 6, Name = "Chronic Kidney Disease", Category = "Renal", Status = Status.Active }
            );

            // Medications
            modelBuilder.Entity<Medication>().HasData(
                new Medication { Id = 1, Name = "Metformin", Category = "Antidiabetic", Status = Status.Active },
                new Medication { Id = 2, Name = "Lisinopril", Category = "ACE Inhibitor", Status = Status.Active },
                new Medication { Id = 3, Name = "Salbutamol Inhaler", Category = "Bronchodilator", Status = Status.Active },
                new Medication { Id = 4, Name = "Atorvastatin", Category = "Statin", Status = Status.Active },
                new Medication { Id = 5, Name = "Aspirin", Category = "Antiplatelet", Status = Status.Active }
            );

            // Sample Types
            modelBuilder.Entity<SampleType>().HasData(
                new SampleType { Id = 1, Name = "Whole Blood", Status = Status.Active },
                new SampleType { Id = 2, Name = "Plasma", Status = Status.Active },
                new SampleType { Id = 3, Name = "Serum", Status = Status.Active },
                new SampleType { Id = 4, Name = "Bone Marrow", Status = Status.Active },
                new SampleType { Id = 5, Name = "Urine", Status = Status.Active }
            );

            // Test Categories
            modelBuilder.Entity<TestCategory>().HasData(
                new TestCategory { Id = 1, CategoryName = "Full Blood Count", Description = "Complete blood count with differential", Status = Status.Active },
                new TestCategory { Id = 2, CategoryName = "Coagulation Studies", Description = "PT, aPTT, INR", Status = Status.Active },
                new TestCategory { Id = 3, CategoryName = "Peripheral Blood Film", Description = "Morphology examination", Status = Status.Active },
                new TestCategory { Id = 4, CategoryName = "Bone Marrow Aspirate", Description = "Bone marrow analysis", Status = Status.Active },
                new TestCategory { Id = 5, CategoryName = "Haemoglobin Electrophoresis", Description = "Haemoglobin variants", Status = Status.Active }
            );

            // Suppliers
            modelBuilder.Entity<Supplier>().HasData(
                new Supplier { Id = 1, SupplierName = "MediLab Supplies", ContactPerson = "Thabo Ndlovu", EmailAddress = "thabo@medilab.co.za", Status = Status.Active },
                new Supplier { Id = 2, SupplierName = "BioTech Diagnostics", ContactPerson = "Priya Naidoo", EmailAddress = "priya@biotech.co.za", Status = Status.Active },
                new Supplier { Id = 3, SupplierName = "LabCare Solutions", ContactPerson = "Johan van Wyk", EmailAddress = "johan@labcare.co.za", Status = Status.Active }
            );

            // Consumables
            modelBuilder.Entity<Consumable>().HasData(
                new Consumable { Id = 1, ConsumableName = "EDTA Vacutainer (4ml)", ReorderLevel = 100, QuantityOnHand = 250, SupplierId = 1, Status = Status.Active },
                new Consumable { Id = 2, ConsumableName = "Sodium Citrate Tube (2.7ml)", ReorderLevel = 80, QuantityOnHand = 150, SupplierId = 1, Status = Status.Active },
                new Consumable { Id = 3, ConsumableName = "Microscope Slides", ReorderLevel = 200, QuantityOnHand = 500, SupplierId = 2, Status = Status.Active },
                new Consumable { Id = 4, ConsumableName = "Wright-Giemsa Stain Kit", ReorderLevel = 10, QuantityOnHand = 25, SupplierId = 2, Status = Status.Active },
                new Consumable { Id = 5, ConsumableName = "PT/INR Test Strips", ReorderLevel = 50, QuantityOnHand = 80, SupplierId = 3, Status = Status.Active },
                new Consumable { Id = 6, ConsumableName = "Haemoglobin Electrophoresis Buffer", ReorderLevel = 5, QuantityOnHand = 12, SupplierId = 2, Status = Status.Active }
            );

            // Test Types
            modelBuilder.Entity<TestType>().HasData(
                new TestType { Id = 1, TestName = "Haemoglobin", TestCategoryId = 1, SampleTypeId = 1, UnitsOfMeasurement = "g/dL", NormalRangeMin = 12.0m, NormalRangeMax = 16.0m, TurnaroundTimeMinutes = 30, Status = Status.Active },
                new TestType { Id = 2, TestName = "White Blood Cell Count", TestCategoryId = 1, SampleTypeId = 1, UnitsOfMeasurement = "x10³/µL", NormalRangeMin = 4.0m, NormalRangeMax = 11.0m, TurnaroundTimeMinutes = 30, Status = Status.Active },
                new TestType { Id = 3, TestName = "Platelet Count", TestCategoryId = 1, SampleTypeId = 1, UnitsOfMeasurement = "x10³/µL", NormalRangeMin = 150m, NormalRangeMax = 450m, TurnaroundTimeMinutes = 30, Status = Status.Active },
                new TestType { Id = 4, TestName = "Prothrombin Time (PT)", TestCategoryId = 2, SampleTypeId = 2, UnitsOfMeasurement = "seconds", NormalRangeMin = 11.0m, NormalRangeMax = 13.5m, TurnaroundTimeMinutes = 45, Status = Status.Active },
                new TestType { Id = 5, TestName = "Activated Partial Thromboplastin Time (aPTT)", TestCategoryId = 2, SampleTypeId = 2, UnitsOfMeasurement = "seconds", NormalRangeMin = 25.0m, NormalRangeMax = 35.0m, TurnaroundTimeMinutes = 45, Status = Status.Active },
                new TestType { Id = 6, TestName = "Peripheral Blood Smear Review", TestCategoryId = 3, SampleTypeId = 1, UnitsOfMeasurement = null, NormalRangeMin = null, NormalRangeMax = null, TurnaroundTimeMinutes = 60, Status = Status.Active },
                new TestType { Id = 7, TestName = "Bone Marrow Aspirate Analysis", TestCategoryId = 4, SampleTypeId = 4, UnitsOfMeasurement = null, NormalRangeMin = null, NormalRangeMax = null, TurnaroundTimeMinutes = 120, Status = Status.Active },
                new TestType { Id = 8, TestName = "Haemoglobin Electrophoresis", TestCategoryId = 5, SampleTypeId = 1, UnitsOfMeasurement = "%", NormalRangeMin = null, NormalRangeMax = null, TurnaroundTimeMinutes = 90, Status = Status.Active }
            );

            // TestTypeConsumable (Junction)
            modelBuilder.Entity<TestTypeConsumable>().HasData(
                // FBC tests use EDTA tube and slides (for smear)
                new TestTypeConsumable { TestTypeId = 1, ConsumableId = 1, Status = Status.Active },
                new TestTypeConsumable { TestTypeId = 2, ConsumableId = 1, Status = Status.Active },
                new TestTypeConsumable { TestTypeId = 3, ConsumableId = 1, Status = Status.Active },
                // Coagulation uses citrate tube and strips
                new TestTypeConsumable { TestTypeId = 4, ConsumableId = 2, Status = Status.Active },
                new TestTypeConsumable { TestTypeId = 4, ConsumableId = 5, Status = Status.Active },
                new TestTypeConsumable { TestTypeId = 5, ConsumableId = 2, Status = Status.Active },
                new TestTypeConsumable { TestTypeId = 5, ConsumableId = 5, Status = Status.Active },
                // Smear uses slides and stain
                new TestTypeConsumable { TestTypeId = 6, ConsumableId = 3, Status = Status.Active },
                new TestTypeConsumable { TestTypeId = 6, ConsumableId = 4, Status = Status.Active },
                // Bone marrow uses slides and stain
                new TestTypeConsumable { TestTypeId = 7, ConsumableId = 3, Status = Status.Active },
                new TestTypeConsumable { TestTypeId = 7, ConsumableId = 4, Status = Status.Active },
                // Electrophoresis uses buffer and EDTA tube
                new TestTypeConsumable { TestTypeId = 8, ConsumableId = 1, Status = Status.Active },
                new TestTypeConsumable { TestTypeId = 8, ConsumableId = 6, Status = Status.Active }
            );






        }
    }
}