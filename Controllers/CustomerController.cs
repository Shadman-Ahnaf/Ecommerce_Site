using System.Security.Claims;
using Ecom.ViewModels;
using EcomDB.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecom.Controllers
{
    [Authorize(Roles = "Buyer")]
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // CUSTOMER PROFILE
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            int userId = GetCurrentUserId();

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return RedirectToAction("CustomerLogin", "Account");

            var model = new CustomerProfileViewModel
            {
                UserId = user.UserId,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                CreatedAt = user.CreatedAt
            };

            return View(model);
        }

        // UPDATE CUSTOMER PROFILE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(CustomerProfileViewModel model)
        {
            int userId = GetCurrentUserId();

            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return RedirectToAction("CustomerLogin", "Account");

            string email = model.Email.Trim().ToLower();
            string? phone = string.IsNullOrWhiteSpace(model.Phone)
                ? null
                : model.Phone.Trim();

            bool emailExists = await _context.Users
                .AnyAsync(u => u.UserId != userId && u.Email.ToLower() == email);

            if (emailExists)
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                return View(model);
            }

            if (phone != null)
            {
                bool phoneExists = await _context.Users
                    .AnyAsync(u => u.UserId != userId && u.Phone == phone);

                if (phoneExists)
                {
                    ModelState.AddModelError("Phone", "This phone number is already registered.");
                    return View(model);
                }
            }

            user.Name = model.Name.Trim();
            user.Email = email;
            user.Phone = phone;

            await _context.SaveChangesAsync();

            TempData["Message"] = "Your profile has been updated successfully.";

            return RedirectToAction(nameof(Profile));
        }

        // CUSTOMER SETTINGS
        [HttpGet]
        public IActionResult Settings()
        {
            return View();
        }

        private int GetCurrentUserId()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (int.TryParse(userId, out int id))
                return id;

            return 0;
        }
    }
}