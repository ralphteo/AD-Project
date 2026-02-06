using ADWebApplication.Data;
using ADWebApplication.Models;
using ADWebApplication.Services;
using ADWebApplication.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ADWebApplication.Controllers;

public class EmpAuthController : Controller
{
    private readonly In5niteDbContext _db;
    private readonly IEmailService _email;

    // OTP session keys
    private const string KEY_OTP = "OTP";
    private const string KEY_USER = "OTPUsername";
    private const string KEY_EXP = "OTPExpiry";
    private const int OTP_MAX_ATTEMPTS = 5;
    private static readonly TimeSpan LOCK_DURATION = TimeSpan.FromMinutes(30);

    public EmpAuthController(In5niteDbContext db, IEmailService email)
    {
        _db = db;
        _email = email;
    }

    // Helpers (PER USER session keys)
    private static string AttemptsKey(string username) => $"OTPAttemptsLeft:{username}";
    private static string LockUntilKey(string username) => $"OTPLockUntil:{username}";

    private bool IsUserLocked(string username, out DateTime lockUntilUtc)
    {
        lockUntilUtc = default;
        if (string.IsNullOrWhiteSpace(username)) return false;

        var lockStr = HttpContext.Session.GetString(LockUntilKey(username));
        if (string.IsNullOrEmpty(lockStr)) return false;

        if (!DateTime.TryParse(lockStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out lockUntilUtc))
        {
            HttpContext.Session.Remove(LockUntilKey(username));
            return false;
        }

        if (DateTime.UtcNow < lockUntilUtc) return true;

        HttpContext.Session.Remove(LockUntilKey(username));
        return false;
    }

    private int GetAttemptsLeft(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return OTP_MAX_ATTEMPTS;
        return HttpContext.Session.GetInt32(AttemptsKey(username)) ?? OTP_MAX_ATTEMPTS;
    }

    private void ResetAttempts(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return;
        HttpContext.Session.SetInt32(AttemptsKey(username), OTP_MAX_ATTEMPTS);
    }

    private void DecrementAttemptsOrLock(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return;

        int attemptsLeft = GetAttemptsLeft(username);
        attemptsLeft--;

        HttpContext.Session.SetInt32(AttemptsKey(username), attemptsLeft);

        if (attemptsLeft <= 0)
        {
            var lockUntil = DateTime.UtcNow.Add(LOCK_DURATION);
            HttpContext.Session.SetString(LockUntilKey(username), lockUntil.ToString("O"));

            HttpContext.Session.Remove(KEY_OTP);
            HttpContext.Session.Remove(KEY_EXP);
        }
    }

    private void StartOtpSession(string username, string otp)
    {
        HttpContext.Session.SetString(KEY_USER, username);
        HttpContext.Session.SetString(KEY_OTP, otp);
        HttpContext.Session.SetString(KEY_EXP, DateTime.UtcNow.AddMinutes(1).ToString("O"));

        ResetAttempts(username);
    }

    private void ClearOtpSessionOnly()
    {
        HttpContext.Session.Remove(KEY_OTP);
        HttpContext.Session.Remove(KEY_EXP);
        HttpContext.Session.Remove(KEY_USER);
    }

    // Login
    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
            return RedirectToAction(nameof(RouteAfterLogin));

        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var loginUsername = (model.EmployeeId ?? "").Trim();

        if (IsUserLocked(loginUsername, out var lockUntilUtc))
        {
            ModelState.AddModelError("", $"Account locked until {lockUntilUtc.ToLocalTime():HH:mm}.");
            return View(model);
        }

        var user = await _db.Employees
            .Include(e => e.Role)
            .FirstOrDefaultAsync(e => e.Username == loginUsername && e.IsActive);

        if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError("", "Invalid Employee ID or Password.");
            return View(model);
        }

        string otp = new Random().Next(100000, 999999).ToString();
        StartOtpSession(user.Username, otp);

        try
        {
            await _email.SendOtpEmail(user.Email, otp);
        }
        catch
        {
            ClearOtpSessionOnly();
            ModelState.AddModelError("", "Unable to send OTP email. Please try again.");
            return View(model);
        }

        TempData["Message"] = "OTP has been sent. It is valid for 1 minute.";
        return RedirectToAction(nameof(VerifyOtp));
    }

    // Verify OTP (GET)
    [HttpGet]
    public IActionResult VerifyOtp()
    {
        var username = HttpContext.Session.GetString(KEY_USER);

        if (string.IsNullOrEmpty(username))
            return RedirectToAction(nameof(Login));

        if (IsUserLocked(username, out var lockUntilUtc))
        {
            TempData["Message"] = $"Account locked until {lockUntilUtc.ToLocalTime():HH:mm}.";
            ClearOtpSessionOnly();
            return RedirectToAction(nameof(Login));
        }

        if (string.IsNullOrEmpty(HttpContext.Session.GetString(KEY_OTP)))
            return RedirectToAction(nameof(Login));

        ViewBag.AttemptsLeft = GetAttemptsLeft(username);
        return View(new OtpVerificationViewModel());
    }

    // Verify OTP (POST)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyOtp(OtpVerificationViewModel model)
    {
        var username = HttpContext.Session.GetString(KEY_USER);

        if (string.IsNullOrEmpty(username))
            return RedirectToAction(nameof(Login));

        if (IsUserLocked(username, out var lockUntilUtc))
        {
            ModelState.AddModelError("", $"Account locked until {lockUntilUtc.ToLocalTime():HH:mm}.");
            ViewBag.AttemptsLeft = 0;
            return View(model);
        }

        var savedOtp = HttpContext.Session.GetString(KEY_OTP);
        var expiryStr = HttpContext.Session.GetString(KEY_EXP);

        if (string.IsNullOrEmpty(savedOtp) || string.IsNullOrEmpty(expiryStr))
        {
            ClearOtpSessionOnly();
            ModelState.AddModelError("", "Session expired. Please login again.");
            return View(model);
        }

        if (!DateTime.TryParse(expiryStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expiryUtc))
        {
            ClearOtpSessionOnly();
            ModelState.AddModelError("", "Session invalid. Please login again.");
            return View(model);
        }

        if (DateTime.UtcNow > expiryUtc)
        {
            ClearOtpSessionOnly();
            ModelState.AddModelError("", "OTP expired. Please login again.");
            return View(model);
        }

        string inputOtp = new string((model.OtpCode ?? "").Where(char.IsDigit).ToArray());

        if (inputOtp != savedOtp)
        {
            DecrementAttemptsOrLock(username);

            if (IsUserLocked(username, out var untilUtc2))
            {
                ModelState.AddModelError("", $"Too many wrong OTP. Locked until {untilUtc2.ToLocalTime():HH:mm}.");
                ViewBag.AttemptsLeft = 0;
                return View(model);
            }

            ViewBag.AttemptsLeft = GetAttemptsLeft(username);
            ModelState.AddModelError("", $"Invalid OTP. Attempts left: {ViewBag.AttemptsLeft}");
            return View(model);
        }

        // correct OTP
        ClearOtpSessionOnly();
        ResetAttempts(username);

        var user = await _db.Employees
            .Include(e => e.Role)
            .FirstOrDefaultAsync(e => e.Username == username && e.IsActive);

        if (user == null)
        {
            ModelState.AddModelError("", "User account not active.");
            return View(model);
        }

        await SignInUser(user);
        return RedirectToAction(nameof(RouteAfterLogin));
    }

    // Resend OTP
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendOtp()
    {
        var username = HttpContext.Session.GetString(KEY_USER);
        if (string.IsNullOrEmpty(username))
            return Json(new { success = false, message = "Session expired. Please login again." });

        // locked? block resend
        if (IsUserLocked(username, out var lockUntilUtc))
            return Json(new { success = false, message = $"Account locked until {lockUntilUtc.ToLocalTime():HH:mm}. Cannot resend OTP." });

        var user = await _db.Employees.FirstOrDefaultAsync(e => e.Username == username && e.IsActive);
        if (user == null)
            return Json(new { success = false, message = "User not found." });

        string otp = new Random().Next(100000, 999999).ToString();
        StartOtpSession(username, otp); // resets attempts too

        await _email.SendOtpEmail(user.Email, otp);
        return Json(new { success = true, message = "OTP resent to your email." });
    }

    // Route after login
    [HttpGet]
    public async Task<IActionResult> RouteAfterLogin()
    {
        if (User.Identity == null || !User.Identity.IsAuthenticated)
            return RedirectToAction(nameof(Login));

        var username = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(username))
            return RedirectToAction(nameof(Login));

        var user = await _db.Employees
            .Include(e => e.Role)
            .FirstOrDefaultAsync(e => e.Username == username && e.IsActive);

        if (user?.Role == null)
            return RedirectToAction(nameof(Login));

        return user.Role.Name switch
        {
            "HR" => RedirectToAction("Index", "Hr"),
            "Admin" => RedirectToAction("Index", "AdminDashboard"),
            "Collector" => RedirectToAction("Index", "CollectorDashboard"),
            _ => RedirectToAction(nameof(Login))
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    private async Task SignInUser(Employee user)
    {
        var roleName = user.Role?.Name ?? "Employee";

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Username),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("FullName", user.FullName),
            new Claim(ClaimTypes.Role, roleName)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
            }
        );
    }
}
