using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Ecom.DTOs.Auth;
using EcomDB.Data;
using Ecom.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecom.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }


        // ============================================================
        // CUSTOMER REGISTER
        // ============================================================

        [HttpGet]
        public IActionResult CustomerRegister()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CustomerRegister(
            RegisterRequestDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string email =
                model.Email.Trim().ToLower();

            string? phone =
                string.IsNullOrWhiteSpace(model.Phone)
                    ? null
                    : model.Phone.Trim();

            bool emailExists =
                _context.Users.Any(u =>
                    u.Email.ToLower() == email);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "Email",
                    "This email is already registered.");

                return View(model);
            }

            if (phone != null)
            {
                bool phoneExists =
                    _context.Users.Any(u =>
                        u.Phone == phone);

                if (phoneExists)
                {
                    ModelState.AddModelError(
                        "Phone",
                        "This phone number is already registered.");

                    return View(model);
                }
            }

            User user = new User
            {
                Name = model.Name.Trim(),

                Email = email,

                Phone = phone,

                PasswordHash =
                    HashPassword(model.Password),

                Role = UserRole.Buyer,

                IsActive = true,

                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);

            _context.SaveChanges();

            TempData["Message"] =
                "Account created successfully! Please sign in.";

            return RedirectToAction(
                "CustomerLogin");
        }


        // ============================================================
        // CUSTOMER LOGIN
        // ============================================================

        [HttpGet]
        public IActionResult CustomerLogin()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CustomerLogin(
            LoginRequestDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string loginValue =
                model.EmailOrPhone.Trim();

            User? user;

            if (loginValue.Contains("@"))
            {
                string email =
                    loginValue.ToLower();

                user =
                    await _context.Users
                        .FirstOrDefaultAsync(u =>
                            u.Email.ToLower() == email);
            }
            else
            {
                user =
                    await _context.Users
                        .FirstOrDefaultAsync(u =>
                            u.Phone == loginValue);
            }

            if (user == null ||
                user.PasswordHash !=
                HashPassword(model.Password))
            {
                ModelState.AddModelError(
                    "",
                    "Invalid email/phone or password.");

                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(
                    "",
                    "Your account is currently inactive.");

                return View(model);
            }

            if (user.Role != UserRole.Buyer)
            {
                ModelState.AddModelError(
                    "",
                    "This login is for customer accounts only.");

                return View(model);
            }

            await SignInUser(user);

            TempData["Message"] =
                $"Welcome back, {user.Name}!";

            return RedirectToAction(
                "Shop",
                "Home");
        }


        // ============================================================
        // SELLER REGISTER - GET
        // ============================================================

        [HttpGet]
        public IActionResult SellerRegister()
        {
            return View();
        }


        // ============================================================
        // SELLER REGISTER - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SellerRegister(
            RegisterRequestDto model,
            string BusinessName,
            string BusinessDescription,
            string NIDDocumentUrl,
            string BankDocumentUrl,
            string? BusinessDocumentUrl)
        {
            // --------------------------------------------------------
            // BASIC VALIDATION
            // --------------------------------------------------------

            if (!ModelState.IsValid)
                return View(model);

            if (string.IsNullOrWhiteSpace(BusinessName))
            {
                ModelState.AddModelError(
                    "BusinessName",
                    "Business name is required.");

                return View(model);
            }

            if (string.IsNullOrWhiteSpace(BusinessDescription))
            {
                ModelState.AddModelError(
                    "BusinessDescription",
                    "Business description is required.");

                return View(model);
            }

            if (string.IsNullOrWhiteSpace(NIDDocumentUrl))
            {
                ModelState.AddModelError(
                    "NIDDocumentUrl",
                    "NID document is required.");

                return View(model);
            }

            if (string.IsNullOrWhiteSpace(BankDocumentUrl))
            {
                ModelState.AddModelError(
                    "BankDocumentUrl",
                    "Bank document is required.");

                return View(model);
            }


            // --------------------------------------------------------
            // NORMALIZE DATA
            // --------------------------------------------------------

            string email =
                model.Email.Trim().ToLower();

            string? phone =
                string.IsNullOrWhiteSpace(model.Phone)
                    ? null
                    : model.Phone.Trim();

            string businessName =
                BusinessName.Trim();

            string businessDescription =
                BusinessDescription.Trim();

            string nidDocumentUrl =
                NIDDocumentUrl.Trim();

            string bankDocumentUrl =
                BankDocumentUrl.Trim();

            string? businessDocumentUrl =
                string.IsNullOrWhiteSpace(BusinessDocumentUrl)
                    ? null
                    : BusinessDocumentUrl.Trim();


            // --------------------------------------------------------
            // CHECK EMAIL
            // --------------------------------------------------------

            bool emailExists =
                await _context.Users
                    .AnyAsync(u =>
                        u.Email.ToLower() == email);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "Email",
                    "This email is already registered.");

                return View(model);
            }


            // --------------------------------------------------------
            // CHECK PHONE
            // --------------------------------------------------------

            if (phone != null)
            {
                bool phoneExists =
                    await _context.Users
                        .AnyAsync(u =>
                            u.Phone == phone);

                if (phoneExists)
                {
                    ModelState.AddModelError(
                        "Phone",
                        "This phone number is already registered.");

                    return View(model);
                }
            }


            // --------------------------------------------------------
            // CREATE USER
            //
            // Pending seller remains inactive and Buyer role
            // until an administrator approves the application.
            // --------------------------------------------------------

            User user = new User
            {
                Name = model.Name.Trim(),

                Email = email,

                Phone = phone,

                PasswordHash =
                    HashPassword(model.Password),

                Role = UserRole.Buyer,

                IsActive = false,

                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();


            // --------------------------------------------------------
            // CREATE SELLER APPLICATION
            // --------------------------------------------------------

            SellerApplication application =
                new SellerApplication
                {
                    UserId = user.UserId,

                    BusinessName = businessName,

                    BusinessDescription =
                        businessDescription,

                    NIDDocumentUrl =
                        nidDocumentUrl,

                    BankDocumentUrl =
                        bankDocumentUrl,

                    BusinessDocumentUrl =
                        businessDocumentUrl,

                    Status = "Pending",

                    AppliedAt = DateTime.UtcNow
                };

            _context.SellerApplications.Add(application);

            await _context.SaveChangesAsync();


            // --------------------------------------------------------
            // SEND SELLER TO THANK YOU PAGE
            // --------------------------------------------------------

            return RedirectToAction(
                nameof(SellerApplicationSubmitted));
        }


        // ============================================================
        // SELLER APPLICATION SUBMITTED
        // ============================================================

        [HttpGet]
        public IActionResult SellerApplicationSubmitted()
        {
            return View();
        }


        // ============================================================
        // SELLER LOGIN
        // ============================================================

        [HttpGet]
        public IActionResult SellerLogin()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SellerLogin(
            LoginRequestDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string loginValue =
                model.EmailOrPhone.Trim();

            User? user;

            if (loginValue.Contains("@"))
            {
                string email =
                    loginValue.ToLower();

                user =
                    await _context.Users
                        .FirstOrDefaultAsync(u =>
                            u.Email.ToLower() == email);
            }
            else
            {
                user =
                    await _context.Users
                        .FirstOrDefaultAsync(u =>
                            u.Phone == loginValue);
            }

            if (user == null ||
                user.PasswordHash !=
                HashPassword(model.Password))
            {
                ModelState.AddModelError(
                    "",
                    "Invalid seller credentials.");

                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(
                    "",
                    "Your seller application may still be under review or your account is inactive.");

                return View(model);
            }

            if (user.Role != UserRole.Seller)
            {
                ModelState.AddModelError(
                    "",
                    "This account is not an approved seller account.");

                return View(model);
            }

            SellerProfile? seller =
                await _context.SellerProfiles
                    .FirstOrDefaultAsync(s =>
                        s.UserId == user.UserId);

            if (seller == null)
            {
                ModelState.AddModelError(
                    "",
                    "Seller profile was not found.");

                return View(model);
            }

            await SignInUser(user);

            TempData["Message"] =
                $"Welcome back, {user.Name}!";

            return RedirectToAction(
                "Index",
                "Seller");
        }


        // ============================================================
        // ADMIN LOGIN
        // ============================================================

        [HttpGet]
        public IActionResult AdminLogin()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminLogin(
            LoginRequestDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string email =
                model.EmailOrPhone.Trim().ToLower();

            User? user =
                await _context.Users
                    .FirstOrDefaultAsync(u =>
                        u.Email.ToLower() == email);

            if (user == null ||
                user.PasswordHash !=
                HashPassword(model.Password))
            {
                ModelState.AddModelError(
                    "",
                    "Invalid admin email or password.");

                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(
                    "",
                    "This admin account is inactive.");

                return View(model);
            }

            if (user.Role != UserRole.Admin)
            {
                ModelState.AddModelError(
                    "",
                    "This account does not have administrator privileges.");

                return View(model);
            }

            Admin? admin =
                await _context.Admins
                    .FirstOrDefaultAsync(a =>
                        a.UserId == user.UserId);

            if (admin == null)
            {
                ModelState.AddModelError(
                    "",
                    "Admin profile was not found.");

                return View(model);
            }

            await SignInUser(user);

            TempData["Message"] =
                $"Welcome back, {user.Name}!";

            return RedirectToAction(
                "Index",
                "Admin");
        }


        // ============================================================
        // LOGOUT
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("EcomCookie");

            TempData["Message"] =
                "You have been logged out.";

            return RedirectToAction(
                "CustomerLogin");
        }


        // ============================================================
        // ACCESS DENIED
        // ============================================================

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return Content(
                "You do not have permission to access this page.");
        }


        // ============================================================
        // SIGN IN
        // ============================================================

        private async Task SignInUser(User user)
        {
            List<Claim> claims = new()
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.UserId.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    user.Name),

                new Claim(
                    ClaimTypes.Email,
                    user.Email),

                new Claim(
                    ClaimTypes.Role,
                    user.Role.ToString())
            };

            ClaimsIdentity identity =
                new ClaimsIdentity(
                    claims,
                    "EcomCookie");

            ClaimsPrincipal principal =
                new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                "EcomCookie",
                principal);
        }


        // ============================================================
        // PASSWORD HASHING
        // ============================================================

        private string HashPassword(string password)
        {
            using SHA256 sha256 =
                SHA256.Create();

            byte[] bytes =
                sha256.ComputeHash(
                    Encoding.UTF8.GetBytes(password));

            StringBuilder builder =
                new StringBuilder();

            foreach (byte b in bytes)
            {
                builder.Append(
                    b.ToString("x2"));
            }

            return builder.ToString();
        }
    }
}