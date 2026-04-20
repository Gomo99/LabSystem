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

        // Medical history lookup tables
        public DbSet<MedicalCondition> MedicalConditions { get; set; }
        public DbSet<Allergy> Allergies { get; set; }
        public DbSet<Medication> Medications { get; set; }

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
            // 1. Global Enum → String Conversion
            // ------------------------------------------------------------
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType.IsEnum)
                    {
                        var converterType = typeof(EnumToStringConverter<>).MakeGenericType(property.ClrType);
                        var converter = (ValueConverter)Activator.CreateInstance(converterType)!;
                        property.SetValueConverter(converter);
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

            // Doctor: HPCSA number unique (only when role is Doctor and value not null)
            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.HPCSANumber)
                .IsUnique()
                .HasFilter("[HPCSANumber] IS NOT NULL AND [Role] = 'Doctor'");

            // Lab Technician: SA ID number unique (only when role is LabTechnician and value not null)
            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.SAIDNumber)
                .IsUnique()
                .HasFilter("[SAIDNumber] IS NOT NULL AND [Role] = 'LabTechnician'");

            modelBuilder.Entity<Sample>()
                .HasIndex(s => s.Barcode)
                .IsUnique();

            // ------------------------------------------------------------
            // 4. Foreign Key Relationships (Explicit for clarity)
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

            // Order → OrderItem
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId);

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

            // ------------------------------------------------------------
            // 5. Default Values for Status / IsActive columns
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
        }
    }
}