using LaboratoryTestRequestManagementSystem.AppStatus;
using LaboratoryTestRequestManagementSystem.ViewModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Crypto.Generators;
using System.Security.Claims;

namespace LaboratoryTestRequestManagementSystem.Controllers
{
    public class Account : Controller
    {
        // ── LOGIN ────────────────────────────────────────────────

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

            // ── Employee path (login by username) ─────────────────
            var employee = _context.Employees
                .FirstOrDefault(e => e.UserName == model.UserName);

            if (employee != null)
            {
                if (employee.IsActive != Status.Active)
                {
                    ModelState.AddModelError(string.Empty, "Invalid username or password.");
                    return View(model);
                }

                if (employee.IsLockedOut)
                {
                    ModelState.AddModelError(string.Empty,
                        $"Account locked. Try again after {employee.LockoutEnd:HH:mm}.");
                    return View(model);
                }

                bool empPasswordValid;
                if (!string.IsNullOrEmpty(employee.PasswordHash)
                    && employee.PasswordHash.StartsWith("$2"))
                {
                    empPasswordValid = BCrypt.Net.BCrypt.Verify(model.Password, employee.PasswordHash);
                }
                else
                {
                    empPasswordValid = model.Password == employee.PasswordHash;
                    if (empPasswordValid)
                    {
                        string upgraded = BCrypt.Net.BCrypt.HashPassword(model.Password);
                        employee.PasswordHash = upgraded;
                        await _context.SaveChangesAsync();
                        await _passwordHistory.SaveEmployeePasswordAsync(employee.EmployeeID, upgraded);
                    }
                }

                if (!empPasswordValid)
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
                            $"Invalid username or password. " +
                            $"{5 - employee.FailedLoginAttempts} attempt(s) remaining.");
                    }

                    await _context.SaveChangesAsync();
                    return View(model);
                }

                employee.FailedLoginAttempts = 0;
                employee.LockoutEnd = null;
                await _context.SaveChangesAsync();

                if (employee.IsTwoFactorEnabled && !string.IsNullOrEmpty(employee.TwoFactorSecretKey))
                {
                    TempData["2fa_pending_id"] = employee.EmployeeID.ToString();
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

            // ── Customer path (login by email) ────────────────────
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == model.UserName);

            if (customer != null)
            {
                if (customer.IsActive != Status.Active)
                {
                    ModelState.AddModelError(string.Empty, "Invalid username or password.");
                    return View(model);
                }

                if (customer.IsLockedOut)
                {
                    ModelState.AddModelError(string.Empty,
                        $"Account locked. Try again after {customer.LockoutEnd:HH:mm}.");
                    return View(model);
                }

                // Google-only accounts have no password — redirect to Google
                if (string.IsNullOrEmpty(customer.PasswordHash) && customer.GoogleId != null)
                {
                    ModelState.AddModelError(string.Empty,
                        "This account uses Google Sign-In. Please use the 'Sign in with Google' button.");
                    return View(model);
                }

                bool custPasswordValid;
                if (!string.IsNullOrEmpty(customer.PasswordHash)
                    && customer.PasswordHash.StartsWith("$2"))
                {
                    custPasswordValid = BCrypt.Net.BCrypt.Verify(model.Password, customer.PasswordHash);
                }
                else
                {
                    custPasswordValid = model.Password == customer.PasswordHash;
                    if (custPasswordValid)
                    {
                        string upgraded = BCrypt.Net.BCrypt.HashPassword(model.Password);
                        customer.PasswordHash = upgraded;
                        await _context.SaveChangesAsync();
                        await _passwordHistory.SaveCustomerPasswordAsync(customer.CustomerId, upgraded);
                    }
                }

                if (!custPasswordValid)
                {
                    customer.FailedLoginAttempts++;
                    if (customer.FailedLoginAttempts >= 5)
                    {
                        customer.LockoutEnd = DateTime.Now.AddMinutes(15);
                        customer.FailedLoginAttempts = 0;
                        ModelState.AddModelError(string.Empty,
                            "Too many failed attempts. Account locked for 15 minutes.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty,
                            $"Invalid email or password. " +
                            $"{5 - customer.FailedLoginAttempts} attempt(s) remaining.");
                    }

                    await _context.SaveChangesAsync();
                    return View(model);
                }

                customer.FailedLoginAttempts = 0;
                customer.LockoutEnd = null;
                await _context.SaveChangesAsync();

                if (customer.IsTwoFactorEnabled && !string.IsNullOrEmpty(customer.TwoFactorSecretKey))
                {
                    TempData["2fa_pending_id"] = customer.CustomerId.ToString();
                    TempData["2fa_pending_type"] = "Customer";
                    TempData["2fa_remember_me"] = model.RememberMe.ToString();
                    TempData["2fa_return_url"] = returnUrl ?? string.Empty;
                    return RedirectToAction("TwoFactorChallenge");
                }

                await SignInAsync(BuildCustomerClaims(customer), model.RememberMe);
                return RedirectToSavedUrl(returnUrl);
            }

            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        // ── LOGOUT ───────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }



        private static int CountRecoveryCodes(string? json)
        {
            if (string.IsNullOrEmpty(json)) return 0;
            try
            {
                return System.Text.Json.JsonSerializer
                    .Deserialize<List<string>>(json)?.Count ?? 0;
            }
            catch { return 0; }
        }

        private string RegenerateQr(string secretKey, string email, ITwoFactorService tfService)
        {
            var uri = tfService.GetQrCodeUri(secretKey, email, "Ibhayi Pharmacy");
            var qrPng = tfService.GenerateQrCodePng(uri);
            return Convert.ToBase64String(qrPng);
        }



        private IActionResult RedirectToDashboard(UserRole? role = null)
        {
            if (role == null && User.Identity?.IsAuthenticated == true)
            {
                var roleStr = User.FindFirstValue(ClaimTypes.Role);
                if (Enum.TryParse<UserRole>(roleStr, out var parsed))
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



    }
}
