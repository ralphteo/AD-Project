using ADWebApplication.Data;
using ADWebApplication.Models;
using ADWebApplication.Services;
using ADWebApplication.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.Design;
using System.Data.Common;
using System.Reflection.Metadata;
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
        if(User.Identity != null && User.Identity.IsAuthenticated)
            return RedirectToAction(nameof(RouteAfterLogin));
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
            if (!ModelState.IsValid) return View(model);

            var user = await _db.EmpAccounts
                .FirstOrDefaultAsync(u => u.Username == model.EmployeeId && u.IsActive);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Invalid Employee ID or Password.");
                return View(model);
            }

            string otp = new Random().Next(100000, 999999).ToString();

            HttpContext.Session.SetString(KEY_OTP, otp);
            HttpContext.Session.SetString(KEY_USER, user.Username);
            HttpContext.Session.SetString(KEY_EXP, DateTime.Now.AddMinutes(1).ToString("O"));

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

            TempData["Message"] = "OTP has been sent. It is valid for 1 minutes.";
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

            // expiry
            var expiry = DateTime.Parse(expiryStr, null, System.Globalization.DateTimeStyles.RoundtripKind);
            if (DateTime.Now > expiry)
            {
                HttpContext.Session.Clear();
                ModelState.AddModelError("", "OTP expired. Please login again.");
                return View(model);
            }

            // check otp
            if (model.OtpCode != savedOtp)
            {
                ModelState.AddModelError("", "Invalid OTP.");
                return View(model);
            }

            // OTP success
            HttpContext.Session.Clear();

            // load admin again (DB)
            var user = await _db.EmpAccounts.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
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

            var user = await _db.EmpAccounts.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            string otp = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString(KEY_OTP, otp);
            HttpContext.Session.SetString(KEY_EXP, DateTime.Now.AddMinutes(1).ToString("O"));

            await _email.SendOtpEmail(user.Email, otp);
            return Json(new { success = true, message = "OTP resent to your email." });
        }
    [HttpGet]
    public async Task<IActionResult> RouteAfterLogin()
    {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return RedirectToAction(nameof(Login));

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return RedirectToAction(nameof(Login));

            bool isHr = await _db.EmpRoles.AnyAsync(ur => ur.EmpAccountId == userId && ur.Role.Name == "HR");
            if (isHr) return RedirectToAction("CreateEmployee", "Hr");

            bool isAdmin = await _db.EmpRoles.AnyAsync(ur => ur.EmpAccountId == userId && ur.Role.Name == "Admin");
            if (isAdmin) return RedirectToAction("Index", "AdminDashboard");

            return RedirectToAction("Index", "CollectorDashboard");
    }
   
   [HttpPost]
   [ValidateAntiForgeryToken]
   public async Task<IActionResult> Logout()
    {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
    }
    private async Task SignInUser(EmpAccount user)
        {
            var roles = await _db.EmpRoles
                .Where(ur => ur.EmpAccountId == user.Id)
                .Select(ur => ur.Role.Name)
                .ToListAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("FullName", user.FullName)
            };

            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                }
            );

        }
}