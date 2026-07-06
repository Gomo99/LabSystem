# Laboratory Test Request Management System

A secure, multi‑role web application built with ASP.NET Core MVC for managing laboratory test requests, results, and patient data. The system fully supports **Admins**, **Doctors**, **Lab Technicians**, **Laboratory Managers**, and **Patients**. Robust authentication with two‑factor authentication, device trust, comprehensive workflows for test ordering, sample processing, result verification, quality control, patient self‑service, and data portability are provided.

---

## Features

### Authentication & Security
- Multi‑role login for Employees (Admin, Doctor, Lab Manager, Lab Technician) and Patients.
- BCrypt password hashing with automatic upgrade of legacy hashes.
- Account lockout after 5 consecutive failed attempts.
- TOTP‑based Two‑Factor Authentication (QR setup, recovery codes, device trust).
- Forgot/Reset password flow via email.
- Self‑service password and username change.
- Google external login (auto‑registers new patients).
- Device trust management and revocation.
- Soft account deactivation for staff.

### Admin Module
- **Medical Conditions, Allergies & Medications catalog**  
  CRUD operations with search, filter by category, and soft delete/restore.

### Doctor Module
- **Patient Management**  
  Register new patients (auto‑generated password, email notification), edit demographic and medical history (conditions, allergies, medications with auto‑suggest), soft delete/restore.
- **Test Request Workflow**  
  - Create test request with multiple test types and sample entries (barcodes validated for uniqueness).  
  - Dynamic sample type requirement based on selected test types (AJAX endpoint).  
  - Edit submitted requests (samples editable only when status is “Submitted”).  
  - Cancel requests (with mandatory reason).  
  - View detailed status of each test request with results per test.
- **Result Management**  
  - Release completed results to patient via email (optional appointment request, PDF attachment).  
  - Download results as PDF.  
  - Email results manually.
- **Alerts & Reports**  
  - View list of abnormal test results within a date range.  
  - Generate a Doctor Test Requests Report (PDF) for a custom period.
- **PDF Access Requests**  
  - Approve or deny patient requests to download result PDFs, with notification sent to the patient.

### Laboratory Manager Module
- **Test Category & Test Type Management**  
  CRUD for test categories and test types, including assignment of consumables and sample types. Uniqueness validation on names. Soft delete/restore.
- **Consumables & Suppliers Management**  
  - Manage consumables with reorder levels, stock quantities, and supplier linkage.  
  - Stock adjustment (increase/decrease/set).  
  - Supplier CRUD with uniqueness checks on name and email. Soft delete/restore.
- **Staff Management**  
  - **Doctors** – Create, edit, soft‑delete/restore. Auto‑generated temporary password, email notification.  
  - **Lab Technicians** – Create, edit (including assignment to multiple test types), soft‑delete/restore. Auto‑generated password, email notification.
- **Inventory & Ordering**  
  - Low‑stock alerts based on reorder levels.  
  - Create purchase orders directly from low‑stock view, grouped by supplier.  
  - Edit orders (add/remove items) while status is “Ordered”.  
  - Mark order items as received (automatically updates stock), partial receipt support.  
  - Cancel entire orders or individual items with mandatory reason and email notification to supplier.  
  - Soft delete/restore for orders.
- **Reports**  
  - Generate a Test Performance Report (PDF) for a custom date range.

### Lab Technician Module
- **Dashboard with Filters**  
  Real‑time counts and lists for:  
  - Selected tests (assigned and in‑progress)  
  - Tests waiting for selection (qualified, not started)  
  - Tests waiting for verification (completed by another tech)  
  - Tests returned for review  
  - Urgent (STAT) tests  
  - Overdue and nearing turnaround limit tests  
  Filters for urgency, category, due time, and request number.
- **Sample Reception**  
  - View all submitted requests; receive samples individually or by barcode scan.  
  - Bulk receive samples for a request, marking each sample as received.  
  - Automatically advances request status to “SamplesReceived”.
- **Test Request Management**  
  - Soft delete/restore of test requests (only when in early stages).  
  - Cancel a request with reason, notifying the doctor via email and in‑app notification.
- **Test Processing & Result Capture**  
  - View eligible test types for a request; start a test (deducts consumables, assigns technician).  
  - Capture numeric results with automatic abnormal flagging based on normal ranges.  
  - Complete test to set completion time and status.
- **Verification & Quality Review**  
  - Qualified technicians can verify a test completed by another technician (cannot self‑verify).  
  - Return a test for review with mandatory notes; original technician is notified.  
  - Original technician can resubmit after review with adjusted results.  
  - Full review history logged.
- **Reports**  
  - Generate a Completed Tests Report (PDF) for the logged‑in technician over a date range.
- **Doctor Notification**  
  - When all tests of a request are verified, an email with the results PDF is automatically sent to the requesting doctor.

### Patient Module (Self‑Service Portal)
- **Registration**  
  Self‑registration with email and ID number uniqueness checks, password complexity enforcement, and welcome email.
- **Profile Management**  
  Update personal details (name, contact, address) with validation.
- **Medical History**  
  Self‑manage conditions, allergies, and medications (auto‑suggest from catalog).
- **Test Requests & Results**  
  - View list of own test requests with status and doctor information.  
  - View detailed results per test only when the doctor has released them.  
  - Request access to download the official results PDF; doctor approval required.  
  - Once granted, download the PDF.
- **Result Tracking**  
  Graph historical numeric results for a selected test type, with normal range boundaries.
- **Consent Management**  
  Grant/revoke access to specific doctors for viewing selected test requests; email notification sent to doctor.
- **Reports**  
  Generate a PDF report of own released results within a date range.
- **Data Portability**  
  - Export all personal data (profile, medical history, results) as a JSON file.  
  - Export a QR code containing the same data for quick sharing.  
  - Import previously exported JSON data to update profile, medical history, and external test results.
- **PDF Access Requests**  
  Patients can request permission from their doctor to download result PDFs; notifications are sent.

### Notification System
- In‑app notification bell with unread count (via AJAX).
- View all notifications, mark individual or all as read, delete, or clear all.
- Notifications are integrated across modules for key events (account creation, test status changes, access requests, etc.).

---

## User Roles

| Role                 | Permissions                                                                                             |
|----------------------|---------------------------------------------------------------------------------------------------------|
| **Admin**            | Manage medical condition, allergy, and medication catalogs.                                            |
| **Doctor**           | Register/edit patients, create/edit/cancel test requests, view/release results, generate reports, handle PDF access requests. |
| **LaboratoryManager**| Manage test catalogue, consumables, suppliers, orders, and staff (doctors & technicians). View low‑stock alerts, generate reports. |
| **LabTechnician**    | Receive samples, process assigned tests, capture results, participate in verification/review workflow, view dashboard, generate reports. |
| **Patient**          | Self‑registration, profile & medical history management, view results (when released), track results, grant/revoke doctor access, data export/import, request PDF downloads. |

---

## Technology Stack

- **Backend:** ASP.NET Core MVC (.NET 6/7/8), Entity Framework Core
- **Database:** SQL Server (LocalDB, Express, or full edition)
- **Frontend:** Razor Views, Bootstrap 5, jQuery, AJAX
- **Authentication:** Cookie Authentication, Google OAuth 2.0
- **2FA:** TOTP (RFC 6238) with SHA‑1, BCrypt hashing
- **Email:** Custom `IEmailService` (SMTP ready, supports attachments)
- **PDF:** `IPdfReportService` (implementation customizable)
- **Notifications:** `INotificationService` (in‑app)
- **QR Code Generation:** QRCoder library

---

## Setup & Installation

### Prerequisites
- [.NET SDK](https://dotnet.microsoft.com/download) (6.0 or later)
- SQL Server (LocalDB or higher)
- Visual Studio 2022 / VS Code / Rider
- An email service configuration (SMTP, SendGrid, etc.)

### Steps

1. **Clone the repository**
   ```bash
   git clone https://github.com/your-org/lab-test-management.git
   cd lab-test-management