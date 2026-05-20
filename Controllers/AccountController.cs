using LaboratoryTestRequestManagementSystem.AppStatus;
using LaboratoryTestRequestManagementSystem.Data;
using LaboratoryTestRequestManagementSystem.Models;
using LaboratoryTestRequestManagementSystem.Services;
using LaboratoryTestRequestManagementSystem.ViewModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LaboratoryTestRequestManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly LabDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ITwoFactorService _twoFactorService;
        private const string DeviceIdCookieName = ".DeviceId";

        // Standardized TempData keys
        private const string SuccessMessageKey = "SuccessMessage";
        private const string ErrorMessageKey = "ErrorMessage";

        public AccountController(LabDbContext context, IEmailService emailService, ITwoFactorService twoFactorService)
        {
            _context = context;
            _emailService = emailService;
            _twoFactorService = twoFactorService;
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

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return null;
            return userId;
        }

        private ClaimsPrincipal BuildEmployeeClaims(Employee employee)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, employee.Id.ToString()),
                new Claim(ClaimTypes.Name, employee.Email),
                new Claim(ClaimTypes.Email, employee.Email),
                new Claim(ClaimTypes.GivenName, employee.FirstName),
                new Claim(ClaimTypes.Surname, employee.LastName),
                new Claim(ClaimTypes.Role, employee.Role.ToString())
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(identity);
        }

        private ClaimsPrincipal BuildPatientClaims(Patient patient)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, patient.Id.ToString()),
                new Claim(ClaimTypes.Name, patient.Email),
                new Claim(ClaimTypes.Email, patient.Email),
                new Claim(ClaimTypes.GivenName, patient.FirstName),
                new Claim(ClaimTypes.Surname, patient.LastName),
                new Claim(ClaimTypes.Role, UserRole.Patient.ToString())
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(identity);
        }

        private async Task SignInAsync(ClaimsPrincipal principal, bool isPersistent)
        {
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = isPersistent,
                    ExpiresUtc = isPersistent ? DateTimeOffset.UtcNow.AddDays(7) : null
                });
        }

        private IActionResult RedirectToDashboard(UserRole? role = null)
        {
            if (role == null && User.Identity?.IsAuthenticated == true)
            {
                var roleStr = User.FindFirstValue(ClaimTypes.Role);
                Enum.TryParse<UserRole>(roleStr, out var parsed);
                role = parsed;
            }

            return role switch
            {
                UserRole.Admin => RedirectToAction("Dashboard", "Admin"),
                UserRole.Doctor => RedirectToAction("Dashboard", "Doctor"),
                UserRole.LaboratoryManager => RedirectToAction("Dashboard", "LaboratoryManager"),
                UserRole.LabTechnician => RedirectToAction("Dashboard", "LabTechnician"),
                UserRole.Patient => RedirectToAction("Dashboard", "Patient"),
                _ => RedirectToAction("Login", "Account")
            };
        }

        private IActionResult RedirectToSavedUrl(string? returnUrl, UserRole? role = null)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToDashboard(role);
        }

        private static string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);

        private static bool VerifyAndUpgradePassword(string password, ref string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash))
                return false;

            if (storedHash.StartsWith("$2"))
                return BCrypt.Net.BCrypt.Verify(password, storedHash);

            if (password == storedHash)
            {
                storedHash = BCrypt.Net.BCrypt.HashPassword(password);
                return true;
            }

            return false;
        }

        private static bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrEmpty(hash)) return false;
            if (hash.StartsWith("$2"))
                return BCrypt.Net.BCrypt.Verify(password, hash);
            return password == hash;
        }

        private static bool IsPasswordComplex(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8) return false;
            bool hasUpper = Regex.IsMatch(password, @"[A-Z]");
            bool hasDigit = Regex.IsMatch(password, @"\d");
            bool hasSpecial = Regex.IsMatch(password, @"[^a-zA-Z0-9\s]");
            return hasUpper && hasDigit && hasSpecial;
        }

        private static int CountRecoveryCodes(string? json)
        {
            if (string.IsNullOrEmpty(json)) return 0;
            try { return JsonSerializer.Deserialize<List<string>>(json)?.Count ?? 0; }
            catch { return 0; }
        }

        private string GetOrCreateDeviceId()
        {
            if (Request.Cookies.TryGetValue(DeviceIdCookieName, out var existingId))
                return existingId;

            var newId = Guid.NewGuid().ToString("N");
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            };
            Response.Cookies.Append(DeviceIdCookieName, newId, cookieOptions);
            return newId;
        }

        private async Task<UserDevice> GetOrCreateDeviceRecord(int userId, string userType, string deviceId, string? ipAddress)
        {
            var device = await _context.UserDevices
                .FirstOrDefaultAsync(d => d.DeviceId == deviceId && d.UserId == userId && d.UserType == userType);

            if (device == null)
            {
                device = new UserDevice
                {
                    UserId = userId,
                    UserType = userType,
                    DeviceId = deviceId,
                    DeviceName = Request.Headers["User-Agent"].ToString(),
                    IpAddress = ipAddress,
                    IsTrusted = false,
                    FirstSeen = DateTime.Now,
                    LastSeen = DateTime.Now
                };
                _context.UserDevices.Add(device);
            }
            else
            {
                device.LastSeen = DateTime.Now;
                device.IpAddress = ipAddress;
            }
            await _context.SaveChangesAsync();
            return device;
        }

        private string GenerateTemporaryIdNumber()
        {
            var rnd = new Random();
            return DateTime.Now.ToString("yyMMdd") + rnd.Next(1000, 9999).ToString();
        }

        // ======================================================================
        //  LOGIN (supports both Employee and Patient)
        // ======================================================================
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToDashboard();

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Try Employee
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Email == model.Username);

            if (employee != null)
            {
                if (employee.IsActive != Status.Active)
                {
                    SetError("Account is inactive.");
                    return View(model);
                }

                if (employee.LockoutEnd.HasValue && employee.LockoutEnd > DateTime.Now)
                {
                    SetError($"Account locked. Try again after {employee.LockoutEnd:HH:mm}.");
                    return View(model);
                }

                string passwordHash = employee.PasswordHash;
                bool passwordValid = VerifyAndUpgradePassword(model.Password, ref passwordHash);
                if (passwordHash != employee.PasswordHash)
                {
                    employee.PasswordHash = passwordHash;
                }
                if (!passwordValid)
                {
                    employee.FailedLoginAttempts++;
                    if (employee.FailedLoginAttempts >= 5)
                    {
                        employee.LockoutEnd = DateTime.Now.AddMinutes(15);
                        employee.FailedLoginAttempts = 0;
                        SetError("Too many failed attempts. Account locked for 15 minutes.");
                    }
                    else
                    {
                        SetError($"Invalid email or password. {5 - employee.FailedLoginAttempts} attempt(s) remaining.");
                    }
                    await _context.SaveChangesAsync();
                    return View(model);
                }

                employee.FailedLoginAttempts = 0;
                employee.LockoutEnd = null;
                await _context.SaveChangesAsync();

                // Get or create device record
                string deviceId = GetOrCreateDeviceId();
                var device = await GetOrCreateDeviceRecord(employee.Id, "Employee", deviceId,
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                // 2FA check
                if (employee.IsTwoFactorEnabled && !string.IsNullOrEmpty(employee.TwoFactorSecretKey))
                {
                    if (device.IsTrusted)
                    {
                        await SignInAsync(BuildEmployeeClaims(employee), model.RememberMe);

                        if (employee.MustChangePassword)
                            return RedirectToAction("ChangePassword");

                        return RedirectToSavedUrl(returnUrl, employee.Role);
                    }

                    TempData["2fa_pending_id"] = employee.Id.ToString();
                    TempData["2fa_pending_type"] = "Employee";
                    TempData["2fa_remember_me"] = model.RememberMe.ToString();
                    TempData["2fa_return_url"] = returnUrl ?? string.Empty;
                    return RedirectToAction("TwoFactorChallenge");
                }

                await SignInAsync(BuildEmployeeClaims(employee), model.RememberMe);

                if (employee.MustChangePassword)
                    return RedirectToAction("ChangePassword");

                return RedirectToSavedUrl(returnUrl, employee.Role);
            }

            // Try Patient
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Email == model.Username);

            if (patient != null)
            {
                if (patient.IsActive != Status.Active)
                {
                    SetError("Account is inactive.");
                    return View(model);
                }

                if (patient.LockoutEnd.HasValue && patient.LockoutEnd > DateTime.Now)
                {
                    SetError($"Account locked. Try again after {patient.LockoutEnd:HH:mm}.");
                    return View(model);
                }

                string patientPasswordHash = patient.PasswordHash;
                bool passwordValid = VerifyAndUpgradePassword(model.Password, ref patientPasswordHash);
                if (patientPasswordHash != patient.PasswordHash)
                {
                    patient.PasswordHash = patientPasswordHash;
                }
                if (!passwordValid)
                {
                    patient.FailedLoginAttempts++;
                    if (patient.FailedLoginAttempts >= 5)
                    {
                        patient.LockoutEnd = DateTime.Now.AddMinutes(15);
                        patient.FailedLoginAttempts = 0;
                        SetError("Too many failed attempts. Account locked for 15 minutes.");
                    }
                    else
                    {
                        SetError($"Invalid email or password. {5 - patient.FailedLoginAttempts} attempt(s) remaining.");
                    }
                    await _context.SaveChangesAsync();
                    return View(model);
                }

                patient.FailedLoginAttempts = 0;
                patient.LockoutEnd = null;
                await _context.SaveChangesAsync();

                await SignInAsync(BuildPatientClaims(patient), model.RememberMe);

                if (patient.MustChangePassword)
                    return RedirectToAction("ChangePassword");

                return RedirectToSavedUrl(returnUrl, UserRole.Patient);
            }

            SetError("Invalid email or password.");
            return View(model);
        }

        // ======================================================================
        //  TWO‑FACTOR CHALLENGE (after password verified)
        // ======================================================================
        [HttpGet]
        public IActionResult TwoFactorChallenge()
        {
            if (TempData["2fa_pending_id"] == null)
                return RedirectToAction("Login");

            var model = new TwoFactorChallengeViewModel
            {
                ReturnUrl = TempData["2fa_return_url"]?.ToString() ?? string.Empty
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TwoFactorChallenge(TwoFactorChallengeViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var pendingId = TempData["2fa_pending_id"]?.ToString();
            var pendingType = TempData["2fa_pending_type"]?.ToString();
            var rememberMe = TempData["2fa_remember_me"]?.ToString() == "True";
            var returnUrl = TempData["2fa_return_url"]?.ToString();

            if (string.IsNullOrEmpty(pendingId) || pendingType != "Employee")
                return RedirectToAction("Login");

            if (!int.TryParse(pendingId, out int employeeId))
                return RedirectToAction("Login");

            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null)
                return RedirectToAction("Login");

            bool isValid = false;

            if (model.UseRecoveryCode)
            {
                if (!string.IsNullOrEmpty(model.RecoveryCode))
                {
                    isValid = _twoFactorService.VerifyRecoveryCode(
                        employee.TwoFactorRecoveryCodes ?? string.Empty,
                        model.RecoveryCode,
                        out string updatedJson);
                    if (isValid)
                        employee.TwoFactorRecoveryCodes = updatedJson;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(model.Code))
                {
                    isValid = _twoFactorService.VerifyCode(employee.TwoFactorSecretKey!, model.Code);
                }
            }

            if (!isValid)
            {
                SetError("Invalid authentication code. Please try again.");
                TempData["2fa_pending_id"] = employee.Id.ToString();
                TempData["2fa_pending_type"] = "Employee";
                TempData["2fa_remember_me"] = rememberMe.ToString();
                TempData["2fa_return_url"] = returnUrl ?? string.Empty;
                return View(model);
            }

            if (model.TrustDevice)
            {
                string deviceId = GetOrCreateDeviceId();
                var device = await _context.UserDevices
                    .FirstOrDefaultAsync(d => d.DeviceId == deviceId && d.UserId == employeeId && d.UserType == "Employee");
                if (device != null)
                {
                    device.IsTrusted = true;
                }
            }

            await _context.SaveChangesAsync();
            await SignInAsync(BuildEmployeeClaims(employee), rememberMe);

            TempData.Remove("2fa_pending_id");
            TempData.Remove("2fa_pending_type");
            TempData.Remove("2fa_remember_me");
            TempData.Remove("2fa_return_url");

            if (employee.MustChangePassword)
                return RedirectToAction("ChangePassword");

            return RedirectToSavedUrl(returnUrl, employee.Role);
        }

        // ======================================================================
        //  TWO‑FACTOR SETUP (enable 2FA)
        // ======================================================================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SetupTwoFactor()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login");

            var employee = await _context.Employees.FindAsync(userId.Value);
            if (employee == null) return RedirectToAction("Login");

            if (employee.IsTwoFactorEnabled)
                return RedirectToAction("ManageTwoFactor");

            var secretKey = _twoFactorService.GenerateSecretKey();
            employee.TwoFactorSecretKey = secretKey;
            await _context.SaveChangesAsync();

            var uri = _twoFactorService.GetQrCodeUri(secretKey, employee.Email, "NMB-HLabSys");
            var qrBytes = _twoFactorService.GenerateQrCodePng(uri);
            var qrBase64 = Convert.ToBase64String(qrBytes);

            var model = new TwoFactorSetupViewModel
            {
                SecretKey = secretKey,
                QrCodeBase64 = qrBase64
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetupTwoFactor(TwoFactorSetupViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login");

            var employee = await _context.Employees.FindAsync(userId.Value);
            if (employee == null) return RedirectToAction("Login");

            if (!ModelState.IsValid)
            {
                var uri = _twoFactorService.GetQrCodeUri(employee.TwoFactorSecretKey!, employee.Email, "NMB-HLabSys");
                var qrBytes = _twoFactorService.GenerateQrCodePng(uri);
                model.QrCodeBase64 = Convert.ToBase64String(qrBytes);
                model.SecretKey = employee.TwoFactorSecretKey!;
                return View(model);
            }

            bool isValid = _twoFactorService.VerifyCode(employee.TwoFactorSecretKey!, model.VerificationCode);
            if (!isValid)
            {
                SetError("Invalid verification code. Please try again.");
                var uri = _twoFactorService.GetQrCodeUri(employee.TwoFactorSecretKey!, employee.Email, "NMB-HLabSys");
                var qrBytes = _twoFactorService.GenerateQrCodePng(uri);
                model.QrCodeBase64 = Convert.ToBase64String(qrBytes);
                model.SecretKey = employee.TwoFactorSecretKey!;
                return View(model);
            }

            employee.IsTwoFactorEnabled = true;
            var recoveryCodes = _twoFactorService.GenerateRecoveryCodes();
            var hashedCodes = recoveryCodes.Select(c => HashPassword(c)).ToList();
            employee.TwoFactorRecoveryCodes = JsonSerializer.Serialize(hashedCodes);

            await _context.SaveChangesAsync();

            TempData["ShowRecoveryCodes"] = "true";
            return RedirectToAction("ShowRecoveryCodes", new { codes = string.Join(",", recoveryCodes) });
        }

        [Authorize]
        [HttpGet]
        public IActionResult ShowRecoveryCodes(string codes)
        {
            if (string.IsNullOrEmpty(codes))
                return RedirectToAction("ManageTwoFactor");

            var model = new TwoFactorRecoveryCodesViewModel
            {
                PlainCodes = codes.Split(',').ToList()
            };
            return View(model);
        }

        // ======================================================================
        //  MANAGE 2FA (view status, regenerate codes, disable)
        // ======================================================================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ManageTwoFactor()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login");

            var employee = await _context.Employees.FindAsync(userId.Value);
            if (employee == null) return RedirectToAction("Login");

            var model = new ManageTwoFactorViewModel
            {
                IsTwoFactorEnabled = employee.IsTwoFactorEnabled,
                RecoveryCodesLeft = CountRecoveryCodes(employee.TwoFactorRecoveryCodes)
            };
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegenerateRecoveryCodes()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login");

            var employee = await _context.Employees.FindAsync(userId.Value);
            if (employee == null) return RedirectToAction("Login");

            if (!employee.IsTwoFactorEnabled)
                return RedirectToAction("SetupTwoFactor");

            var recoveryCodes = _twoFactorService.GenerateRecoveryCodes();
            var hashedCodes = recoveryCodes.Select(c => HashPassword(c)).ToList();
            employee.TwoFactorRecoveryCodes = JsonSerializer.Serialize(hashedCodes);
            await _context.SaveChangesAsync();

            TempData["ShowRecoveryCodes"] = "true";
            return RedirectToAction("ShowRecoveryCodes", new { codes = string.Join(",", recoveryCodes) });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableTwoFactor(string password)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login");

            var employee = await _context.Employees.FindAsync(userId.Value);
            if (employee == null) return RedirectToAction("Login");

            if (!VerifyPassword(password, employee.PasswordHash))
            {
                SetError("Password is incorrect.");
                return RedirectToAction("ManageTwoFactor");
            }

            employee.IsTwoFactorEnabled = false;
            employee.TwoFactorSecretKey = null;
            employee.TwoFactorRecoveryCodes = null;
            await _context.SaveChangesAsync();

            SetSuccess("Two-factor authentication has been disabled.");
            return RedirectToAction("ManageTwoFactor");
        }

        // ======================================================================
        //  DEVICE MANAGEMENT (trusted devices)
        // ======================================================================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ManageDevices()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login");

            var userType = User.FindFirstValue(ClaimTypes.Role) == UserRole.Patient.ToString() ? "Patient" : "Employee";

            var devices = await _context.UserDevices
                .Where(d => d.UserId == userId && d.UserType == userType)
                .OrderByDescending(d => d.LastSeen)
                .ToListAsync();

            return View(devices);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeDevice(int deviceId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login");

            var userType = User.FindFirstValue(ClaimTypes.Role) == UserRole.Patient.ToString() ? "Patient" : "Employee";

            var device = await _context.UserDevices
                .FirstOrDefaultAsync(d => d.Id == deviceId && d.UserId == userId && d.UserType == userType);

            if (device != null)
            {
                _context.UserDevices.Remove(device);
                await _context.SaveChangesAsync();
                SetSuccess("Device removed.");
            }

            return RedirectToAction("ManageDevices");
        }

        // ======================================================================
        //  FORGOT / RESET PASSWORD
        // ======================================================================
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == model.Email);
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Email == model.Email);

            if (employee == null && patient == null)
            {
                // Don't reveal user existence, but still show confirmation
                SetSuccess("If an account exists, a password reset link has been sent.");
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            string token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            DateTime expiry = DateTime.Now.AddHours(1);

            if (employee != null)
            {
                employee.ResetToken = token;
                employee.ResetTokenExpiry = expiry;
                await _context.SaveChangesAsync();

                string resetLink = Url.Action("ResetPassword", "Account",
                    new { email = employee.Email, token }, Request.Scheme)!;
                await _emailService.SendEmailAsync(employee.Email, "Password Reset Request",
                    $"Please reset your password by clicking this link: {resetLink}");
            }
            else if (patient != null)
            {
                patient.ResetToken = token;
                patient.ResetTokenExpiry = expiry;
                await _context.SaveChangesAsync();

                string resetLink = Url.Action("ResetPassword", "Account",
                    new { email = patient.Email, token }, Request.Scheme)!;
                await _emailService.SendEmailAsync(patient.Email, "Password Reset Request",
                    $"Please reset your password by clicking this link: {resetLink}");
            }

            SetSuccess("If an account exists, a password reset link has been sent.");
            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation() => View();

        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
                return RedirectToAction("Login");

            var model = new ResetPasswordViewModel { Email = email, Token = token };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (!IsPasswordComplex(model.Password))
            {
                SetError("Password must be at least 8 characters and contain an uppercase letter, a number, and a special character.");
                return View(model);
            }

            var employee = await _context.Employees.FirstOrDefaultAsync(e =>
                e.Email == model.Email && e.ResetToken == model.Token && e.ResetTokenExpiry > DateTime.Now);

            var patient = await _context.Patients.FirstOrDefaultAsync(p =>
                p.Email == model.Email && p.ResetToken == model.Token && p.ResetTokenExpiry > DateTime.Now);

            if (employee == null && patient == null)
            {
                SetError("Invalid or expired token.");
                return View(model);
            }

            if (employee != null)
            {
                employee.PasswordHash = HashPassword(model.Password);
                employee.ResetToken = null;
                employee.ResetTokenExpiry = null;
                employee.MustChangePassword = false;
                await _context.SaveChangesAsync();
            }
            else if (patient != null)
            {
                patient.PasswordHash = HashPassword(model.Password);
                patient.ResetToken = null;
                patient.ResetTokenExpiry = null;
                patient.MustChangePassword = false;
                await _context.SaveChangesAsync();
            }

            SetSuccess("Your password has been reset successfully. Please login.");
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation() => View();

        // ======================================================================
        //  CHANGE PASSWORD / CHANGE USERNAME
        // ======================================================================
        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            // Determine the correct dashboard URL for the Cancel button
            string dashboardUrl = Url.Action("Dashboard", GetDashboardControllerName());
            ViewData["DashboardUrl"] = dashboardUrl;
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                SetError("Please fix the validation errors.");
                return View(model);
            }

            if (!IsPasswordComplex(model.NewPassword))
            {
                SetError("Password must be at least 8 characters and contain an uppercase letter, a number, and a special character.");
                return View(model);
            }

            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var roleClaim = User.FindFirstValue(ClaimTypes.Role);

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    SetError("Session expired. Please login again.");
                    return RedirectToAction("Login");
                }

                if (roleClaim == UserRole.Patient.ToString())
                {
                    var patient = await _context.Patients.FindAsync(userId);
                    if (patient == null) return RedirectToAction("Login");

                    if (!VerifyPassword(model.CurrentPassword, patient.PasswordHash))
                    {
                        SetError("Current password is incorrect.");
                        return View(model);
                    }

                    patient.PasswordHash = HashPassword(model.NewPassword);
                    patient.MustChangePassword = false;
                }
                else
                {
                    var employee = await _context.Employees.FindAsync(userId);
                    if (employee == null) return RedirectToAction("Login");

                    if (!VerifyPassword(model.CurrentPassword, employee.PasswordHash))
                    {
                        SetError("Current password is incorrect.");
                        return View(model);
                    }

                    employee.PasswordHash = HashPassword(model.NewPassword);
                    employee.MustChangePassword = false;
                }

                await _context.SaveChangesAsync();

                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Role, roleClaim ?? "")
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties { IsPersistent = true }
                );

                SetSuccess("Password changed successfully.");

                return RedirectToDashboard();
            }
            catch (Exception ex)
            {
                SetError("Something went wrong: " + ex.Message);
                return View(model);
            }
        }

        [Authorize]
        [HttpGet]
        public IActionResult ChangeUsername() => View();

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeUsername(ChangeUsernameViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login");

            var roleClaim = User.FindFirstValue(ClaimTypes.Role);

            bool emailTaken = await _context.Employees.AnyAsync(e => e.Email == model.NewEmail && e.Id != userId)
                           || await _context.Patients.AnyAsync(p => p.Email == model.NewEmail && p.Id != userId);

            if (emailTaken)
            {
                SetError("This email address is already in use.");
                return View(model);
            }

            if (roleClaim == UserRole.Patient.ToString())
            {
                var patient = await _context.Patients.FindAsync(userId);
                if (patient == null) return RedirectToAction("Login");
                patient.Email = model.NewEmail;
                await _context.SaveChangesAsync();
                await SignInAsync(BuildPatientClaims(patient), isPersistent: true);
            }
            else
            {
                var employee = await _context.Employees.FindAsync(userId);
                if (employee == null) return RedirectToAction("Login");
                employee.Email = model.NewEmail;
                employee.Username = model.NewEmail;
                await _context.SaveChangesAsync();
                await SignInAsync(BuildEmployeeClaims(employee), isPersistent: true);
            }

            SetSuccess("Username (email) changed successfully.");
            return RedirectToDashboard();
        }

        // ======================================================================
        //  PROFILE EDIT & DEACTIVATE (Admin, Doctor, LaboratoryManager, LabTechnician)
        // ======================================================================
        [Authorize(Roles = "Admin,Doctor,LaboratoryManager,LabTechnician")]
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login");

            var employee = await _context.Employees.FindAsync(userId.Value);
            if (employee == null || (employee.Role != UserRole.Admin && employee.Role != UserRole.Doctor && employee.Role != UserRole.LaboratoryManager && employee.Role != UserRole.LabTechnician))
                return RedirectToAction("Login");

            var model = new EditProfileViewModel
            {
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                ContactNumber = employee.ContactNumber
            };
            return View(model);
        }

        [Authorize(Roles = "Admin,Doctor,LaboratoryManager,LabTechnician")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login");

            var employee = await _context.Employees.FindAsync(userId.Value);
            if (employee == null || (employee.Role != UserRole.Admin && employee.Role != UserRole.Doctor && employee.Role != UserRole.LaboratoryManager && employee.Role != UserRole.LabTechnician))
                return RedirectToAction("Login");

            employee.FirstName = model.FirstName;
            employee.LastName = model.LastName;
            employee.ContactNumber = model.ContactNumber;
            await _context.SaveChangesAsync();

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await SignInAsync(BuildEmployeeClaims(employee), isPersistent: true);

            SetSuccess("Profile updated successfully.");

            return employee.Role switch
            {
                UserRole.Admin => RedirectToAction("Dashboard", "Admin"),
                UserRole.Doctor => RedirectToAction("Dashboard", "Doctor"),
                UserRole.LaboratoryManager => RedirectToAction("Dashboard", "LaboratoryManager"),
                UserRole.LabTechnician => RedirectToAction("Dashboard", "LabTechnician"),
                _ => RedirectToAction("Login", "Account")
            };
        }

        [Authorize(Roles = "Admin,Doctor,LaboratoryManager,LabTechnician")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateAccount()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login");

            var employee = await _context.Employees.FindAsync(userId.Value);
            if (employee == null || (employee.Role != UserRole.Admin && employee.Role != UserRole.Doctor && employee.Role != UserRole.LaboratoryManager && employee.Role != UserRole.LabTechnician))
                return RedirectToAction("Login");

            employee.IsActive = Status.Inactive;
            await _context.SaveChangesAsync();

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();

            SetSuccess("Your account has been deactivated. Contact an administrator to reactivate.");
            return RedirectToAction("Login", "Account");
        }

        // ======================================================================
        //  LOGOUT
        // ======================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();

            SetSuccess("You have been logged out successfully.");
            return RedirectToAction("Login");
        }

        // ======================================================================
        //  EXTERNAL LOGIN (Google SSO)
        // ======================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            if (remoteError != null)
            {
                SetError($"Error from external provider: {remoteError}");
                return RedirectToAction("Login");
            }

            var info = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!info.Succeeded)
                return RedirectToAction("Login");

            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (!result.Succeeded)
                return RedirectToAction("Login");

            var email = result.Principal.FindFirstValue(ClaimTypes.Email);
            var givenName = result.Principal.FindFirstValue(ClaimTypes.GivenName);
            var surname = result.Principal.FindFirstValue(ClaimTypes.Surname);

            if (string.IsNullOrEmpty(email))
            {
                SetError("Email not provided by Google.");
                return RedirectToAction("Login");
            }

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == email);
            if (employee != null)
            {
                if (employee.IsActive != Status.Active)
                {
                    SetError("Account is inactive.");
                    return RedirectToAction("Login");
                }

                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await SignInAsync(BuildEmployeeClaims(employee), isPersistent: true);

                if (employee.MustChangePassword)
                    return RedirectToAction("ChangePassword");

                return RedirectToSavedUrl(returnUrl, employee.Role);
            }

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Email == email);
            if (patient != null)
            {
                if (patient.IsActive != Status.Active)
                {
                    SetError("Account is inactive.");
                    return RedirectToAction("Login");
                }

                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await SignInAsync(BuildPatientClaims(patient), isPersistent: true);

                if (patient.MustChangePassword)
                    return RedirectToAction("ChangePassword");

                return RedirectToSavedUrl(returnUrl, UserRole.Patient);
            }

            // Create new Patient account automatically
            var newPatient = new Patient
            {
                FirstName = givenName ?? "Google",
                LastName = surname ?? "User",
                Email = email,
                SouthAfricanIdNumber = GenerateTemporaryIdNumber(),
                DateOfBirth = DateTime.Now.AddYears(-30),
                CellphoneNumber = "0000000000",
                HomeAddress = "Not provided",
                PasswordHash = HashPassword(Guid.NewGuid().ToString()),
                IsActive = Status.Active,
                MustChangePassword = true,
                FailedLoginAttempts = 0
            };

            _context.Patients.Add(newPatient);
            await _context.SaveChangesAsync();

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await SignInAsync(BuildPatientClaims(newPatient), isPersistent: true);

            SetSuccess("Account created via Google. Please complete your profile.");
            return RedirectToAction("Profile", "Patient");
        }


        private string GetDashboardControllerName()
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            return role switch
            {
                "Admin" => "Admin",
                "Doctor" => "Doctor",
                "LaboratoryManager" => "LaboratoryManager",
                "LabTechnician" => "LabTechnician",
                "Patient" => "Patient",
                _ => "Account" // fallback (should never happen)
            };
        }
    }
}