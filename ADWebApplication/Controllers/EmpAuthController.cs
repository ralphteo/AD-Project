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
    private readonly EmpDbContext _db;
    private readonly IEmailService _email;

    private const string KEY_OTP = "OTP";
    private const string KEY_USER = "OTPUsername";
    private const string KEY_EXP = "OTPExpiry";

    public EmpAuthController(EmpDbContext empDb, IEmailService email)
    {
        _db = empDb;
        _email = email;
    }

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

        var user = await _db.Employees
            .Include(e => e.Role)
            .FirstOrDefaultAsync(e => e.Username == model.EmployeeId && e.IsActive);

        if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError("", "Invalid Employee ID or Password.");
            return View(model);
        }

        string otp = new Random().Next(100000, 999999).ToString();

        HttpContext.Session.SetString(KEY_OTP, otp);
        HttpContext.Session.SetString(KEY_USER, user.Username);
        HttpContext.Session.SetString(KEY_EXP, DateTime.UtcNow.AddMinutes(1).ToString("O"));

        try
        {
            await _email.SendOtpEmail(user.Email, otp);
        }
        catch
        {
            HttpContext.Session.Clear();
            ModelState.AddModelError("", "Unable to send OTP email. Please try again.");
            return View(model);
        }

        TempData["Message"] = "OTP has been sent. It is valid for 1 minute.";
        return RedirectToAction(nameof(VerifyOtp));
    }

    [HttpGet]
    public IActionResult VerifyOtp()
    {
        if (string.IsNullOrEmpty(HttpContext.Session.GetString(KEY_OTP)))
            return RedirectToAction(nameof(Login));

        return View(new OtpVerificationViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyOtp(OtpVerificationViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var savedOtp = HttpContext.Session.GetString(KEY_OTP);
        var username = HttpContext.Session.GetString(KEY_USER);
        var expiryStr = HttpContext.Session.GetString(KEY_EXP);

        if (string.IsNullOrEmpty(savedOtp) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(expiryStr))
        {
            HttpContext.Session.Clear();
            ModelState.AddModelError("", "Session expired. Please login again.");
            return View(model);
        }

        var expiry = DateTime.Parse(expiryStr, null, System.Globalization.DateTimeStyles.RoundtripKind);

        if (DateTime.UtcNow > expiry)
        {
            HttpContext.Session.Clear();
            ModelState.AddModelError("", "OTP expired. Please login again.");
            return View(model);
        }

        string inputOtp = new string((model.OtpCode ?? "").Where(char.IsDigit).ToArray());
        string sessionOtp = new string((savedOtp ?? "").Where(char.IsDigit).ToArray());

        if (inputOtp != sessionOtp)
        {
            ModelState.AddModelError("", "Invalid OTP.");
            return View(model);
        }

        HttpContext.Session.Clear();

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendOtp()
    {
        var username = HttpContext.Session.GetString(KEY_USER);
        if (string.IsNullOrEmpty(username))
            return Json(new { success = false, message = "Session expired. Please login again." });

        var user = await _db.Employees
            .FirstOrDefaultAsync(e => e.Username == username && e.IsActive);

        if (user == null)
            return Json(new { success = false, message = "User not found." });

        string otp = new Random().Next(100000, 999999).ToString();

        HttpContext.Session.SetString(KEY_OTP, otp);
        HttpContext.Session.SetString(KEY_EXP, DateTime.UtcNow.AddMinutes(1).ToString("O"));

        await _email.SendOtpEmail(user.Email, otp);
        return Json(new { success = true, message = "OTP resent to your email." });
    }

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