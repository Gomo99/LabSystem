using LaboratoryTestRequestManagementSystem.AppStatus;
using LaboratoryTestRequestManagementSystem.Models;
using LaboratoryTestRequestManagementSystem.Models.LaboratoryTestRequestManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LaboratoryTestRequestManagementSystem.Data
{
    public class LabDbContext : DbContext
    {
        public LabDbContext(DbContextOptions options) : base(options) { }

        public DbSet<TestResult> TestResults { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<MedicalCondition> MedicalConditions { get; set; }
        public DbSet<Allergy> Allergies { get; set; }
        public DbSet<Medication> Medications { get; set; }
        public DbSet<PatientCondition> PatientConditions { get; set; }
        public DbSet<PatientAllergy> PatientAllergies { get; set; }
        public DbSet<PatientMedication> PatientMedications { get; set; }
        public DbSet<DoctorPatientAccess> DoctorPatientAccesses { get; set; }
        public DbSet<Consumable> Consumables { get; set; }
        public DbSet<TestType> TestTypes { get; set; }
        public DbSet<TestCategory> TestCategories { get; set; }
        public DbSet<TechnicianTestType> TechnicianTestTypes { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<SampleType> SampleTypes { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<TestTypeConsumable> TestTypeConsumables { get; set; }

        public DbSet<TestRequest> TestRequests { get; set; }
        public DbSet<TestRequestTestType> TestRequestTestTypes { get; set; }
        public DbSet<Sample> Samples { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Enum → string conversion
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

            // Composite keys
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

            // Unique constraints
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

            // ✅ New unique constraints for Employee
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


            modelBuilder.Entity<TestCategory>()
    .Property(tc => tc.Status)
    .HasDefaultValue(Status.Active);

            modelBuilder.Entity<TestType>()
                .Property(tt => tt.Status)
                .HasDefaultValue(Status.Active);

            modelBuilder.Entity<Consumable>()
                .Property(c => c.Status)
                .HasDefaultValue(Status.Active);

            modelBuilder.Entity<Supplier>()
                .Property(s => s.Status)
                .HasDefaultValue(Status.Active);

            modelBuilder.Entity<Allergy>()
    .Property(tc => tc.Status)
    .HasDefaultValue(Status.Active);

            modelBuilder.Entity<PatientAllergy>()
                .Property(tt => tt.Status)
                .HasDefaultValue(Status.Active);

            modelBuilder.Entity<Employee>()
                .Property(e => e.IsActive)
                .HasDefaultValue(Status.Active);

            modelBuilder.Entity<MedicalCondition>()
                .Property(s => s.Status)
                .HasDefaultValue(Status.Active);





            modelBuilder.Entity<TestTypeConsumable>()
    .Property(tc => tc.Status)
    .HasDefaultValue(Status.Active);

            modelBuilder.Entity<TechnicianTestType>()
                .Property(tt => tt.Status)
                .HasDefaultValue(Status.Active);

            modelBuilder.Entity<SampleType>()
                .Property(c => c.Status)
                .HasDefaultValue(Status.Active);

            modelBuilder.Entity<PatientMedication>()
                .Property(s => s.Status)
                .HasDefaultValue(Status.Active);







            modelBuilder.Entity<Medication>()
    .Property(tc => tc.Status)
    .HasDefaultValue(Status.Active);

            modelBuilder.Entity<Order>()
                .Property(o => o.Status)
                .HasDefaultValue(Status.Active);

            modelBuilder.Entity<OrderItem>()
                .Property(c => c.Status)
                .HasDefaultValue(Status.Active);

            modelBuilder.Entity<SampleType>()
                .Property(s => s.Status)
                .HasDefaultValue(Status.Active);




            modelBuilder.Entity<TestRequestTestType>()
    .HasKey(trt => new { trt.TestRequestId, trt.TestTypeId });

            modelBuilder.Entity<Sample>()
                .HasIndex(s => s.Barcode)
                .IsUnique();

            modelBuilder.Entity<TestRequest>()
                .Property(tr => tr.RecordStatus)
                .HasDefaultValue(RequestStatus.Submitted);


            modelBuilder.Entity<TestResult>()
    .HasOne(tr => tr.TestRequestTestType)
    .WithMany()  // assuming no navigation back
    .HasForeignKey(tr => tr.TestRequestTestTypeId)
    .OnDelete(DeleteBehavior.Cascade);

        }



    }
}
