using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LaboratoryTestRequestManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Allergies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Allergies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HPCSANumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SAIDNumber = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: true),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Active"),
                    FailedAttempts = table.Column<int>(type: "int", nullable: false),
                    ResetToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResetTokenExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MustChangePassword = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    EmailVerificationTokenHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailVerificationTokenExpires = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsTwoFactorEnabled = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    TwoFactorSecretKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TwoFactorRecoveryCodes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResetPin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResetPinExpiration = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailedLoginAttempts = table.Column<int>(type: "int", nullable: false),
                    LockoutEnd = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MedicalConditions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalConditions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Medications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    UserType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Link = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRead = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SouthAfricanIdNumber = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CellphoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    HomeAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Active"),
                    MustChangePassword = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    ResetToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResetTokenExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FailedLoginAttempts = table.Column<int>(type: "int", nullable: false),
                    LockoutEnd = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SampleTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SampleTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EmailAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TestCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    UserType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FirstSeen = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeen = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsTrusted = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDevices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DoctorPatientAccesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    DoctorId = table.Column<int>(type: "int", nullable: false),
                    GrantedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SharedTestRequestIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorPatientAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorPatientAccesses_Employees_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DoctorPatientAccesses_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientAllergies",
                columns: table => new
                {
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    AllergyId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientAllergies", x => new { x.PatientId, x.AllergyId });
                    table.ForeignKey(
                        name: "FK_PatientAllergies_Allergies_AllergyId",
                        column: x => x.AllergyId,
                        principalTable: "Allergies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientAllergies_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientConditions",
                columns: table => new
                {
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    MedicalConditionId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientConditions", x => new { x.PatientId, x.MedicalConditionId });
                    table.ForeignKey(
                        name: "FK_PatientConditions_MedicalConditions_MedicalConditionId",
                        column: x => x.MedicalConditionId,
                        principalTable: "MedicalConditions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientConditions_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientMedications",
                columns: table => new
                {
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    MedicationId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientMedications", x => new { x.PatientId, x.MedicationId });
                    table.ForeignKey(
                        name: "FK_PatientMedications_Medications_MedicationId",
                        column: x => x.MedicationId,
                        principalTable: "Medications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientMedications_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    DoctorId = table.Column<int>(type: "int", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Urgency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClinicalNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecordStatus = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Active"),
                    DateCancelled = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestRequests_Employees_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestRequests_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Consumables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsumableName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReorderLevel = table.Column<int>(type: "int", nullable: false),
                    QuantityOnHand = table.Column<int>(type: "int", nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consumables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Consumables_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrderStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCompleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateCancelled = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TestName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TestCategoryId = table.Column<int>(type: "int", nullable: false),
                    SampleTypeId = table.Column<int>(type: "int", nullable: false),
                    UnitsOfMeasurement = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NormalRangeMin = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    NormalRangeMax = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Active"),
                    TurnaroundTimeMinutes = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestTypes_SampleTypes_SampleTypeId",
                        column: x => x.SampleTypeId,
                        principalTable: "SampleTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestTypes_TestCategories_TestCategoryId",
                        column: x => x.TestCategoryId,
                        principalTable: "TestCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Samples",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Barcode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TestRequestId = table.Column<int>(type: "int", nullable: false),
                    SampleTypeId = table.Column<int>(type: "int", nullable: false),
                    CollectedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceivedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceivedById = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Samples", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Samples_Employees_ReceivedById",
                        column: x => x.ReceivedById,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Samples_SampleTypes_SampleTypeId",
                        column: x => x.SampleTypeId,
                        principalTable: "SampleTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Samples_TestRequests_TestRequestId",
                        column: x => x.TestRequestId,
                        principalTable: "TestRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    ConsumableId = table.Column<int>(type: "int", nullable: false),
                    QuantityOrdered = table.Column<int>(type: "int", nullable: false),
                    OrderItemStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Active"),
                    DateReceived = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCancelled = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_Consumables_ConsumableId",
                        column: x => x.ConsumableId,
                        principalTable: "Consumables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TechnicianTestTypes",
                columns: table => new
                {
                    TechnicianId = table.Column<int>(type: "int", nullable: false),
                    TestTypeId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicianTestTypes", x => new { x.TechnicianId, x.TestTypeId });
                    table.ForeignKey(
                        name: "FK_TechnicianTestTypes_Employees_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TechnicianTestTypes_TestTypes_TestTypeId",
                        column: x => x.TestTypeId,
                        principalTable: "TestTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestRequestTestTypes",
                columns: table => new
                {
                    TestRequestId = table.Column<int>(type: "int", nullable: false),
                    TestTypeId = table.Column<int>(type: "int", nullable: false),
                    RequestStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecordStatus = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Active"),
                    TechnicianId = table.Column<int>(type: "int", nullable: true),
                    StartDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletionDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedById = table.Column<int>(type: "int", nullable: true),
                    VerifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerificationNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestRequestTestTypes", x => new { x.TestRequestId, x.TestTypeId });
                    table.ForeignKey(
                        name: "FK_TestRequestTestTypes_Employees_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestRequestTestTypes_Employees_VerifiedById",
                        column: x => x.VerifiedById,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestRequestTestTypes_TestRequests_TestRequestId",
                        column: x => x.TestRequestId,
                        principalTable: "TestRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestRequestTestTypes_TestTypes_TestTypeId",
                        column: x => x.TestTypeId,
                        principalTable: "TestTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TestRequestId = table.Column<int>(type: "int", nullable: false),
                    TestTypeId = table.Column<int>(type: "int", nullable: false),
                    ResultValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsAbnormal = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedById = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestResults_Employees_VerifiedById",
                        column: x => x.VerifiedById,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestResults_TestRequests_TestRequestId",
                        column: x => x.TestRequestId,
                        principalTable: "TestRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestResults_TestTypes_TestTypeId",
                        column: x => x.TestTypeId,
                        principalTable: "TestTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestReviewHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TestRequestId = table.Column<int>(type: "int", nullable: false),
                    TestTypeId = table.Column<int>(type: "int", nullable: false),
                    ReviewerId = table.Column<int>(type: "int", nullable: true),
                    ReviewDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestReviewHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestReviewHistories_Employees_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestReviewHistories_TestRequests_TestRequestId",
                        column: x => x.TestRequestId,
                        principalTable: "TestRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestReviewHistories_TestTypes_TestTypeId",
                        column: x => x.TestTypeId,
                        principalTable: "TestTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestTypeConsumables",
                columns: table => new
                {
                    TestTypeId = table.Column<int>(type: "int", nullable: false),
                    ConsumableId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestTypeConsumables", x => new { x.TestTypeId, x.ConsumableId });
                    table.ForeignKey(
                        name: "FK_TestTypeConsumables_Consumables_ConsumableId",
                        column: x => x.ConsumableId,
                        principalTable: "Consumables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestTypeConsumables_TestTypes_TestTypeId",
                        column: x => x.TestTypeId,
                        principalTable: "TestTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Allergies",
                columns: new[] { "Id", "Category", "Name" },
                values: new object[,]
                {
                    { 1, "Antibiotics", "Penicillin" },
                    { 2, "Antibiotics", "Sulfa Drugs" },
                    { 3, "Environmental", "Latex" },
                    { 4, "Food", "Peanuts" },
                    { 5, "Food", "Shellfish" }
                });

            migrationBuilder.InsertData(
                table: "Employees",
                columns: new[] { "Id", "ContactNumber", "Email", "EmailVerificationTokenExpires", "EmailVerificationTokenHash", "EmployeeNumber", "FailedAttempts", "FailedLoginAttempts", "FirstName", "HPCSANumber", "IsTwoFactorEnabled", "LastName", "LockoutEnd", "MustChangePassword", "PasswordHash", "ResetPin", "ResetPinExpiration", "ResetToken", "ResetTokenExpiry", "Role", "SAIDNumber", "TwoFactorRecoveryCodes", "TwoFactorSecretKey", "Username" },
                values: new object[,]
                {
                    { 1, null, "admin@nmbhlabsys.co.za", null, null, null, 0, 0, "System", null, "False", "Administrator", null, "True", "Temp123!", null, null, null, null, "Admin", null, null, null, "admin@nmbhlabsys.co.za" },
                    { 2, null, "labmanager@nmbhlabsys.co.za", null, null, null, 0, 0, "Lab", null, "False", "Manager", null, "True", "Temp123!", null, null, null, null, "LaboratoryManager", null, null, null, "labmanager@nmbhlabsys.co.za" },
                    { 3, "0821112222", "dr.doe@nmbhlabsys.co.za", null, null, null, 0, 0, "John", "MP0123456", "False", "Doe", null, "True", "Temp123!", null, null, null, null, "Doctor", null, null, null, "dr.doe@nmbhlabsys.co.za" },
                    { 4, "0832223333", "tech.smith@nmbhlabsys.co.za", null, null, "TECH001", 0, 0, "Jane", null, "False", "Smith", null, "True", "Temp123!", null, null, null, null, "LabTechnician", "9001010123087", null, null, "tech.smith@nmbhlabsys.co.za" }
                });

            migrationBuilder.InsertData(
                table: "MedicalConditions",
                columns: new[] { "Id", "Category", "Name" },
                values: new object[,]
                {
                    { 1, "Endocrine", "Diabetes Mellitus Type 2" },
                    { 2, "Cardiovascular", "Hypertension" },
                    { 3, "Respiratory", "Asthma" },
                    { 4, "Infectious Disease", "HIV" },
                    { 5, "Infectious Disease", "Tuberculosis" },
                    { 6, "Renal", "Chronic Kidney Disease" }
                });

            migrationBuilder.InsertData(
                table: "Medications",
                columns: new[] { "Id", "Category", "Name" },
                values: new object[,]
                {
                    { 1, "Antidiabetic", "Metformin" },
                    { 2, "ACE Inhibitor", "Lisinopril" },
                    { 3, "Bronchodilator", "Salbutamol Inhaler" },
                    { 4, "Statin", "Atorvastatin" },
                    { 5, "Antiplatelet", "Aspirin" }
                });

            migrationBuilder.InsertData(
                table: "Patients",
                columns: new[] { "Id", "CellphoneNumber", "DateOfBirth", "Email", "FailedLoginAttempts", "FirstName", "HomeAddress", "LastName", "LockoutEnd", "MustChangePassword", "PasswordHash", "ResetToken", "ResetTokenExpiry", "SouthAfricanIdNumber", "Status" },
                values: new object[] { 1, "0843334444", new DateTime(1985, 5, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), "alice.johnson@example.com", 0, "Alice", "123 Main Street, Gqeberha", "Johnson", null, "True", "Temp123!", null, null, "8505050123088", "Active" });

            migrationBuilder.InsertData(
                table: "SampleTypes",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Whole Blood" },
                    { 2, "Plasma" },
                    { 3, "Serum" },
                    { 4, "Bone Marrow" },
                    { 5, "Urine" }
                });

            migrationBuilder.InsertData(
                table: "Suppliers",
                columns: new[] { "Id", "ContactPerson", "EmailAddress", "SupplierName" },
                values: new object[,]
                {
                    { 1, "Thabo Ndlovu", "thabo@medilab.co.za", "MediLab Supplies" },
                    { 2, "Priya Naidoo", "priya@biotech.co.za", "BioTech Diagnostics" },
                    { 3, "Johan van Wyk", "johan@labcare.co.za", "LabCare Solutions" }
                });

            migrationBuilder.InsertData(
                table: "TestCategories",
                columns: new[] { "Id", "CategoryName", "Description" },
                values: new object[,]
                {
                    { 1, "Full Blood Count", "Complete blood count with differential" },
                    { 2, "Coagulation Studies", "PT, aPTT, INR" },
                    { 3, "Peripheral Blood Film", "Morphology examination" },
                    { 4, "Bone Marrow Aspirate", "Bone marrow analysis" },
                    { 5, "Haemoglobin Electrophoresis", "Haemoglobin variants" }
                });

            migrationBuilder.InsertData(
                table: "Consumables",
                columns: new[] { "Id", "ConsumableName", "QuantityOnHand", "ReorderLevel", "SupplierId" },
                values: new object[,]
                {
                    { 1, "EDTA Vacutainer (4ml)", 250, 100, 1 },
                    { 2, "Sodium Citrate Tube (2.7ml)", 150, 80, 1 },
                    { 3, "Microscope Slides", 500, 200, 2 },
                    { 4, "Wright-Giemsa Stain Kit", 25, 10, 2 },
                    { 5, "PT/INR Test Strips", 80, 50, 3 },
                    { 6, "Haemoglobin Electrophoresis Buffer", 12, 5, 2 }
                });

            migrationBuilder.InsertData(
                table: "TestTypes",
                columns: new[] { "Id", "NormalRangeMax", "NormalRangeMin", "SampleTypeId", "TestCategoryId", "TestName", "TurnaroundTimeMinutes", "UnitsOfMeasurement" },
                values: new object[,]
                {
                    { 1, 16.0m, 12.0m, 1, 1, "Haemoglobin", 30, "g/dL" },
                    { 2, 11.0m, 4.0m, 1, 1, "White Blood Cell Count", 30, "x10³/µL" },
                    { 3, 450m, 150m, 1, 1, "Platelet Count", 30, "x10³/µL" },
                    { 4, 13.5m, 11.0m, 2, 2, "Prothrombin Time (PT)", 45, "seconds" },
                    { 5, 35.0m, 25.0m, 2, 2, "Activated Partial Thromboplastin Time (aPTT)", 45, "seconds" },
                    { 6, null, null, 1, 3, "Peripheral Blood Smear Review", 60, null },
                    { 7, null, null, 4, 4, "Bone Marrow Aspirate Analysis", 120, null },
                    { 8, null, null, 1, 5, "Haemoglobin Electrophoresis", 90, "%" }
                });

            migrationBuilder.InsertData(
                table: "TestTypeConsumables",
                columns: new[] { "ConsumableId", "TestTypeId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 1, 2 },
                    { 1, 3 },
                    { 2, 4 },
                    { 5, 4 },
                    { 2, 5 },
                    { 5, 5 },
                    { 3, 6 },
                    { 4, 6 },
                    { 3, 7 },
                    { 4, 7 },
                    { 1, 8 },
                    { 6, 8 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Consumables_ConsumableName",
                table: "Consumables",
                column: "ConsumableName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Consumables_SupplierId",
                table: "Consumables",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorPatientAccesses_DoctorId",
                table: "DoctorPatientAccesses",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorPatientAccesses_PatientId",
                table: "DoctorPatientAccesses",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Email",
                table: "Employees",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_HPCSANumber",
                table: "Employees",
                column: "HPCSANumber",
                unique: true,
                filter: "[HPCSANumber] IS NOT NULL AND [Role] = 'Doctor'");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_SAIDNumber",
                table: "Employees",
                column: "SAIDNumber",
                unique: true,
                filter: "[SAIDNumber] IS NOT NULL AND [Role] = 'LabTechnician'");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ConsumableId",
                table: "OrderItems",
                column: "ConsumableId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_SupplierId",
                table: "Orders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAllergies_AllergyId",
                table: "PatientAllergies",
                column: "AllergyId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientConditions_MedicalConditionId",
                table: "PatientConditions",
                column: "MedicalConditionId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedications_MedicationId",
                table: "PatientMedications",
                column: "MedicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_Email",
                table: "Patients",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_SouthAfricanIdNumber",
                table: "Patients",
                column: "SouthAfricanIdNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Samples_Barcode",
                table: "Samples",
                column: "Barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Samples_ReceivedById",
                table: "Samples",
                column: "ReceivedById");

            migrationBuilder.CreateIndex(
                name: "IX_Samples_SampleTypeId",
                table: "Samples",
                column: "SampleTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Samples_TestRequestId",
                table: "Samples",
                column: "TestRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_SupplierName",
                table: "Suppliers",
                column: "SupplierName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianTestTypes_TestTypeId",
                table: "TechnicianTestTypes",
                column: "TestTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TestCategories_CategoryName",
                table: "TestCategories",
                column: "CategoryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestRequests_DoctorId",
                table: "TestRequests",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_TestRequests_PatientId",
                table: "TestRequests",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_TestRequestTestTypes_TechnicianId",
                table: "TestRequestTestTypes",
                column: "TechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_TestRequestTestTypes_TestTypeId",
                table: "TestRequestTestTypes",
                column: "TestTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TestRequestTestTypes_VerifiedById",
                table: "TestRequestTestTypes",
                column: "VerifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_TestRequestId",
                table: "TestResults",
                column: "TestRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_TestTypeId",
                table: "TestResults",
                column: "TestTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_VerifiedById",
                table: "TestResults",
                column: "VerifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_TestReviewHistories_ReviewerId",
                table: "TestReviewHistories",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_TestReviewHistories_TestRequestId",
                table: "TestReviewHistories",
                column: "TestRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TestReviewHistories_TestTypeId",
                table: "TestReviewHistories",
                column: "TestTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TestTypeConsumables_ConsumableId",
                table: "TestTypeConsumables",
                column: "ConsumableId");

            migrationBuilder.CreateIndex(
                name: "IX_TestTypes_SampleTypeId",
                table: "TestTypes",
                column: "SampleTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TestTypes_TestCategoryId",
                table: "TestTypes",
                column: "TestCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_DeviceId_UserId_UserType",
                table: "UserDevices",
                columns: new[] { "DeviceId", "UserId", "UserType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoctorPatientAccesses");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "PatientAllergies");

            migrationBuilder.DropTable(
                name: "PatientConditions");

            migrationBuilder.DropTable(
                name: "PatientMedications");

            migrationBuilder.DropTable(
                name: "Samples");

            migrationBuilder.DropTable(
                name: "TechnicianTestTypes");

            migrationBuilder.DropTable(
                name: "TestRequestTestTypes");

            migrationBuilder.DropTable(
                name: "TestResults");

            migrationBuilder.DropTable(
                name: "TestReviewHistories");

            migrationBuilder.DropTable(
                name: "TestTypeConsumables");

            migrationBuilder.DropTable(
                name: "UserDevices");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Allergies");

            migrationBuilder.DropTable(
                name: "MedicalConditions");

            migrationBuilder.DropTable(
                name: "Medications");

            migrationBuilder.DropTable(
                name: "TestRequests");

            migrationBuilder.DropTable(
                name: "Consumables");

            migrationBuilder.DropTable(
                name: "TestTypes");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "SampleTypes");

            migrationBuilder.DropTable(
                name: "TestCategories");
        }
    }
}
