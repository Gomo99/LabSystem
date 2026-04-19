using LaboratoryTestRequestManagementSystem.AppStatus;
using LaboratoryTestRequestManagementSystem.Data;
using LaboratoryTestRequestManagementSystem.Models;
using LaboratoryTestRequestManagementSystem.Services;
using LaboratoryTestRequestManagementSystem.ViewModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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

        public AccountController(LabDbContext context, IEmailService emailService, ITwoFactorService twoFactorService)
        {
            _context = context;
            _emailService = emailService;
            _twoFactorService = twoFactorService;
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
                    ModelState.AddModelError(string.Empty, "Account is inactive.");
                    return View(model);
                }

                if (employee.LockoutEnd.HasValue && employee.LockoutEnd > DateTime.Now)
                {
                    ModelState.AddModelError(string.Empty,
                        $"Account locked. Try again after {employee.LockoutEnd:HH:mm}.");
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
                        ModelState.AddModelError(string.Empty,
                            "Too many failed attempts. Account locked for 15 minutes.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty,
                            $"Invalid email or password. {5 - employee.FailedLoginAttempts} attempt(s) remaining.");
                    }
                    await _context.SaveChangesAsync();
                    return View(model);
                }

                employee.FailedLoginAttempts = 0;
                employee.LockoutEnd = null;
                await _context.SaveChangesAsync(); // Save upgraded hash if needed

                // 2FA required?
                if (employee.IsTwoFactorEnabled && !string.IsNullOrEmpty(employee.TwoFactorSecretKey))
                {
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
                    ModelState.AddModelError(string.Empty, "Account is inactive.");
                    return View(model);
                }

                if (patient.LockoutEnd.HasValue && patient.LockoutEnd > DateTime.Now)
                {
                    ModelState.AddModelError(string.Empty,
                        $"Account locked. Try again after {patient.LockoutEnd:HH:mm}.");
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
                        ModelState.AddModelError(string.Empty,
                            "Too many failed attempts. Account locked for 15 minutes.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty,
                            $"Invalid email or password. {5 - patient.FailedLoginAttempts} attempt(s) remaining.");
                    }
                    await _context.SaveChangesAsync();
                    return View(model);
                }

                patient.FailedLoginAttempts = 0;
                patient.LockoutEnd = null;
                await _context.SaveChangesAsync(); // Save upgraded hash if needed

                await SignInAsync(BuildPatientClaims(patient), model.RememberMe);

                if (patient.MustChangePassword)
                    return RedirectToAction("ChangePassword");

                return RedirectToSavedUrl(returnUrl, UserRole.Patient);
            }

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
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
                ModelState.AddModelError(string.Empty, "Invalid authentication code. Please try again.");
                TempData["2fa_pending_id"] = employee.Id.ToString();
                TempData["2fa_pending_type"] = "Employee";
                TempData["2fa_remember_me"] = rememberMe.ToString();
                TempData["2fa_return_url"] = returnUrl ?? string.Empty;
                return View(model);
            }

            await _context.SaveChangesAsync(); // Persist recovery code removal
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
                ModelState.AddModelError(nameof(model.VerificationCode), "Invalid verification code. Please try again.");
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
                TempData["Error"] = "Password is incorrect.";
                return RedirectToAction("ManageTwoFactor");
            }

            employee.IsTwoFactorEnabled = false;
            employee.TwoFactorSecretKey = null;
            employee.TwoFactorRecoveryCodes = null;
            await _context.SaveChangesAsync();

            TempData["Message"] = "Two‑factor authentication has been disabled.";
            return RedirectToAction("ManageTwoFactor");
        }

        // ======================================================================
        //  PATIENT REGISTRATION
        // ======================================================================

      

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
                return RedirectToAction(nameof(ForgotPasswordConfirmation));

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
                ModelState.AddModelError(nameof(model.Password),
                    "Password must be at least 8 characters and contain an uppercase letter, a number, and a special character.");
                return View(model);
            }

            var employee = await _context.Employees.FirstOrDefaultAsync(e =>
                e.Email == model.Email && e.ResetToken == model.Token && e.ResetTokenExpiry > DateTime.Now);

            var patient = await _context.Patients.FirstOrDefaultAsync(p =>
                p.Email == model.Email && p.ResetToken == model.Token && p.ResetTokenExpiry > DateTime.Now);

            if (employee == null && patient == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid or expired token.");
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

            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation() => View();

        // ======================================================================
        //  CHANGE PASSWORD / CHANGE USERNAME
        // ======================================================================

        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword() => View();

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (!IsPasswordComplex(model.NewPassword))
            {
                ModelState.AddModelError(nameof(model.NewPassword),
                    "Password must be at least 8 characters and contain an uppercase letter, a number, and a special character.");
                return View(model);
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var roleClaim = User.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return RedirectToAction("Login");

            if (roleClaim == UserRole.Patient.ToString())
            {
                var patient = await _context.Patients.FindAsync(userId);
                if (patient == null) return RedirectToAction("Login");

                if (!VerifyPassword(model.CurrentPassword, patient.PasswordHash))
                {
                    ModelState.AddModelError(nameof(model.CurrentPassword), "Current password is incorrect.");
                    return View(model);
                }

                patient.PasswordHash = HashPassword(model.NewPassword);
                patient.MustChangePassword = false;
                await _context.SaveChangesAsync();

                await SignInAsync(BuildPatientClaims(patient), isPersistent: true);
            }
            else
            {
                var employee = await _context.Employees.FindAsync(userId);
                if (employee == null) return RedirectToAction("Login");

                if (!VerifyPassword(model.CurrentPassword, employee.PasswordHash))
                {
                    ModelState.AddModelError(nameof(model.CurrentPassword), "Current password is incorrect.");
                    return View(model);
                }

                employee.PasswordHash = HashPassword(model.NewPassword);
                employee.MustChangePassword = false;
                await _context.SaveChangesAsync();

                await SignInAsync(BuildEmployeeClaims(employee), isPersistent: true);
            }

            TempData["Message"] = "Password changed successfully.";
            return RedirectToDashboard();
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
                ModelState.AddModelError(nameof(model.NewEmail), "This email address is already in use.");
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

            TempData["Message"] = "Username (email) changed successfully.";
            return RedirectToDashboard();
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
            return RedirectToAction("Login", "Account");
        }

        // ======================================================================
        //  HELPER METHODS
        // ======================================================================

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

        /// <summary>
        /// Verifies a password against a stored hash. If the stored hash is plain text
        /// and the password matches, the hash is upgraded to BCrypt automatically.
        /// </summary>
        private static bool VerifyAndUpgradePassword(string password, ref string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash))
                return false;

            // BCrypt hash starts with "$2"
            if (storedHash.StartsWith("$2"))
            {
                return BCrypt.Net.BCrypt.Verify(password, storedHash);
            }

            // Legacy plain text comparison
            if (password == storedHash)
            {
                // Upgrade to BCrypt
                storedHash = BCrypt.Net.BCrypt.HashPassword(password);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Simple verification without auto-upgrade (used where no DbContext save is needed).
        /// </summary>
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
    }
}