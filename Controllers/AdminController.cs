using System.Security.Claims;
using Ecom.Models;
using EcomDB.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecom.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // ADMIN DASHBOARD
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.TotalCustomers = await _context.Users
                .CountAsync(u => u.Role == UserRole.Buyer);

            ViewBag.TotalSellers = await _context.Users
                .CountAsync(u => u.Role == UserRole.Seller);

            ViewBag.ActiveCustomers = await _context.Users
                .CountAsync(u =>
                    u.Role == UserRole.Buyer &&
                    u.IsActive);

            ViewBag.ActiveSellers = await _context.Users
                .CountAsync(u =>
                    u.Role == UserRole.Seller &&
                    u.IsActive);

            ViewBag.PendingSellerApplications =
                await _context.SellerApplications
                    .CountAsync(a => a.Status == "Pending");

            ViewBag.ApprovedSellerApplications =
                await _context.SellerApplications
                    .CountAsync(a => a.Status == "Approved");

            ViewBag.RejectedSellerApplications =
                await _context.SellerApplications
                    .CountAsync(a => a.Status == "Rejected");

            ViewBag.TotalProducts =
                await _context.Products.CountAsync();

            ViewBag.PendingProducts =
                await _context.Products
                    .CountAsync(p => p.Status == ProductStatus.Pending);

            ViewBag.ApprovedProducts =
                await _context.Products
                    .CountAsync(p => p.Status == ProductStatus.Approved);

            ViewBag.RejectedProducts =
                await _context.Products
                    .CountAsync(p => p.Status == ProductStatus.Rejected);

            ViewBag.TotalOrders =
                await _context.Orders.CountAsync();

            ViewBag.PendingOrders =
                await _context.Orders.CountAsync(o =>
                    o.OrderStatus == OrderStatus.Pending ||
                    o.OrderStatus == OrderStatus.Confirmed ||
                    o.OrderStatus == OrderStatus.Processing);

            ViewBag.DeliveredOrders =
                await _context.Orders.CountAsync(o =>
                    o.OrderStatus == OrderStatus.Delivered);

            ViewBag.CancelledOrders =
                await _context.Orders.CountAsync(o =>
                    o.OrderStatus == OrderStatus.Cancelled);

            ViewBag.TotalRevenue =
                await _context.Orders
                    .Where(o =>
                        o.OrderStatus != OrderStatus.Cancelled)
                    .Select(o => (decimal?)o.TotalAmount)
                    .SumAsync() ?? 0m;

            // --------------------------------------------------------
            // RECENT SELLER APPLICATIONS
            // --------------------------------------------------------

            ViewBag.RecentApplications =
                await _context.SellerApplications
                    .Include(a => a.User)
                    .OrderByDescending(a => a.AppliedAt)
                    .Take(5)
                    .ToListAsync();

            // --------------------------------------------------------
            // RECENT PRODUCTS
            // --------------------------------------------------------

            ViewBag.RecentProducts =
                await _context.Products
                    .Include(p => p.Seller)
                        .ThenInclude(s => s.User)
                    .Include(p => p.Category)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .ToListAsync();

            // --------------------------------------------------------
            // RECENT ORDERS
            // --------------------------------------------------------

            ViewBag.RecentOrders =
                await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.Payment)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .ToListAsync();

            return View();
        }


        // ============================================================
        // SELLER APPLICATIONS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> SellerApplications()
        {
            var applications = await _context.SellerApplications
                .Include(a => a.User)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            return View(applications);
        }


        [HttpGet]
        public async Task<IActionResult> SellerApplicationDetails(int id)
        {
            var application = await _context.SellerApplications
                .Include(a => a.User)
                .FirstOrDefaultAsync(a =>
                    a.ApplicationId == id);

            if (application == null)
                return NotFound();

            return View(application);
        }


        // ============================================================
        // APPROVE SELLER
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveSeller(int id)
        {
            var application = await _context.SellerApplications
                .Include(a => a.User)
                .FirstOrDefaultAsync(a =>
                    a.ApplicationId == id);

            if (application == null)
                return NotFound();

            if (application.Status != "Pending")
            {
                TempData["ErrorMessage"] =
                    "This seller application has already been reviewed.";

                return RedirectToAction(nameof(SellerApplications));
            }

            int adminUserId = GetCurrentUserId();

            Admin? admin = await _context.Admins
                .FirstOrDefaultAsync(a =>
                    a.UserId == adminUserId);

            if (admin == null)
                return Unauthorized();

            // Update application
            application.Status = "Approved";
            application.ReviewedAt = DateTime.UtcNow;
            application.ReviewedByAdminId = admin.AdminId;

            // Convert user to seller
            application.User.Role = UserRole.Seller;
            application.User.IsActive = true;

            // Create seller profile if it does not already exist
            SellerProfile? existingProfile =
                await _context.SellerProfiles
                    .FirstOrDefaultAsync(s =>
                        s.UserId == application.UserId);

            if (existingProfile == null)
            {
                SellerProfile sellerProfile = new SellerProfile
                {
                    UserId = application.UserId,
                    BusinessName = application.BusinessName,
                    BusinessDescription =
                        application.BusinessDescription,
                    ApprovedDate = DateTime.UtcNow
                };

                _context.SellerProfiles.Add(sellerProfile);
            }
            else
            {
                existingProfile.BusinessName =
                    application.BusinessName;

                existingProfile.BusinessDescription =
                    application.BusinessDescription;

                existingProfile.ApprovedDate =
                    DateTime.UtcNow;
            }

            // Notify seller
            Notification notification = new Notification
            {
                UserId = application.UserId,
                Title = "Seller Application Approved",
                Message =
                    $"Your seller application for \"{application.BusinessName}\" has been approved. You can now access your seller dashboard and start adding products.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Seller application approved successfully.";

            return RedirectToAction(nameof(SellerApplications));
        }


        // ============================================================
        // REJECT SELLER
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectSeller(
            int id,
            string? rejectionReason)
        {
            var application = await _context.SellerApplications
                .Include(a => a.User)
                .FirstOrDefaultAsync(a =>
                    a.ApplicationId == id);

            if (application == null)
                return NotFound();

            if (application.Status != "Pending")
            {
                TempData["ErrorMessage"] =
                    "This seller application has already been reviewed.";

                return RedirectToAction(nameof(SellerApplications));
            }

            int adminUserId = GetCurrentUserId();

            Admin? admin = await _context.Admins
                .FirstOrDefaultAsync(a =>
                    a.UserId == adminUserId);

            if (admin == null)
                return Unauthorized();

            application.Status = "Rejected";
            application.ReviewedAt = DateTime.UtcNow;
            application.ReviewedByAdminId = admin.AdminId;

            string reason =
                string.IsNullOrWhiteSpace(rejectionReason)
                    ? "Your seller application was not approved by the administrator."
                    : rejectionReason.Trim();

            Notification notification = new Notification
            {
                UserId = application.UserId,
                Title = "Seller Application Rejected",
                Message = reason,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Seller application rejected.";

            return RedirectToAction(nameof(SellerApplications));
        }


        // ============================================================
        // PRODUCT MANAGEMENT
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Products(string? status)
        {
            IQueryable<Product> query =
                _context.Products
                    .Include(p => p.Seller)
                        .ThenInclude(s => s.User)
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages);

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (Enum.TryParse<ProductStatus>(
                    status,
                    true,
                    out ProductStatus productStatus))
                {
                    query = query.Where(p =>
                        p.Status == productStatus);
                }
            }

            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.CurrentStatus = status;

            return View(products);
        }


        [HttpGet]
        public async Task<IActionResult> ProductDetails(int id)
        {
            var product = await _context.Products
                .Include(p => p.Seller)
                    .ThenInclude(s => s.User)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.Reviews)
                .Include(p => p.OrderItems)
                    .ThenInclude(oi => oi.Order)
                .FirstOrDefaultAsync(p =>
                    p.ProductId == id);

            if (product == null)
                return NotFound();

            return View(product);
        }


        // ============================================================
        // APPROVE PRODUCT
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Seller)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(p =>
                    p.ProductId == id);

            if (product == null)
                return NotFound();

            if (product.Status != ProductStatus.Pending)
            {
                TempData["ErrorMessage"] =
                    "This product has already been reviewed.";

                return RedirectToAction(nameof(Products));
            }

            int adminUserId = GetCurrentUserId();

            Admin? admin = await _context.Admins
                .FirstOrDefaultAsync(a =>
                    a.UserId == adminUserId);

            if (admin == null)
                return Unauthorized();

            product.Status = ProductStatus.Approved;
            product.IsActive = true;
            product.ReviewedAt = DateTime.UtcNow;
            product.ReviewedByAdminId = admin.AdminId;
            product.RejectionReason = null;
            product.UpdatedAt = DateTime.UtcNow;

            Notification notification = new Notification
            {
                UserId = product.Seller.UserId,
                Title = "Product Approved",
                Message =
                    $"Your product \"{product.Name}\" has been approved and is now live in the store.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Product approved successfully.";

            return RedirectToAction(nameof(Products));
        }


        // ============================================================
        // REJECT PRODUCT
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectProduct(
            int id,
            string? rejectionReason)
        {
            var product = await _context.Products
                .Include(p => p.Seller)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(p =>
                    p.ProductId == id);

            if (product == null)
                return NotFound();

            if (product.Status != ProductStatus.Pending)
            {
                TempData["ErrorMessage"] =
                    "This product has already been reviewed.";

                return RedirectToAction(nameof(Products));
            }

            int adminUserId = GetCurrentUserId();

            Admin? admin = await _context.Admins
                .FirstOrDefaultAsync(a =>
                    a.UserId == adminUserId);

            if (admin == null)
                return Unauthorized();

            string reason =
                string.IsNullOrWhiteSpace(rejectionReason)
                    ? "Product rejected by administrator."
                    : rejectionReason.Trim();

            product.Status = ProductStatus.Rejected;
            product.IsActive = false;
            product.ReviewedAt = DateTime.UtcNow;
            product.ReviewedByAdminId = admin.AdminId;
            product.RejectionReason = reason;
            product.UpdatedAt = DateTime.UtcNow;

            Notification notification = new Notification
            {
                UserId = product.Seller.UserId,
                Title = "Product Rejected",
                Message =
                    $"Your product \"{product.Name}\" was rejected. Reason: {reason}",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Product rejected successfully.";

            return RedirectToAction(nameof(Products));
        }


        // ============================================================
        // CUSTOMER MANAGEMENT
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Customers()
        {
            var customers = await _context.Users
                .AsNoTracking()
                .Where(u => u.Role == UserRole.Buyer)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return View(customers);
        }


        [HttpGet]
        public async Task<IActionResult> CustomerDetails(int id)
        {
            var customer = await _context.Users
                .Include(u => u.Orders)
                .Include(u => u.Reviews)
                .Include(u => u.Cart)
                .Include(u => u.Wishlist)
                .FirstOrDefaultAsync(u =>
                    u.UserId == id &&
                    u.Role == UserRole.Buyer);

            if (customer == null)
                return NotFound();

            return View(customer);
        }


        // ============================================================
        // ACTIVATE / DEACTIVATE CUSTOMER
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCustomerStatus(int id)
        {
            var customer = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.UserId == id &&
                    u.Role == UserRole.Buyer);

            if (customer == null)
                return NotFound();

            customer.IsActive = !customer.IsActive;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                customer.IsActive
                    ? "Customer account activated."
                    : "Customer account deactivated.";

            return RedirectToAction(nameof(Customers));
        }


        // ============================================================
        // DELETE CUSTOMER
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var customer = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.UserId == id &&
                    u.Role == UserRole.Buyer);

            if (customer == null)
                return NotFound();

            bool hasOrders = await _context.Orders
                .AnyAsync(o => o.UserId == id);

            bool hasReviews = await _context.Reviews
                .AnyAsync(r => r.UserId == id);

            bool hasNotifications = await _context.Notifications
                .AnyAsync(n => n.UserId == id);

            bool hasRefreshTokens = await _context.RefreshTokens
                .AnyAsync(r => r.UserId == id);

            if (hasOrders ||
                hasReviews ||
                hasNotifications ||
                hasRefreshTokens)
            {
                TempData["ErrorMessage"] =
                    "This customer has existing records and cannot be permanently deleted. Deactivate the account instead.";

                return RedirectToAction(nameof(Customers));
            }

            _context.Users.Remove(customer);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Customer deleted successfully.";

            return RedirectToAction(nameof(Customers));
        }


        // ============================================================
        // SELLER MANAGEMENT
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Sellers()
        {
            var sellers = await _context.Users
                .AsNoTracking()
                .Where(u => u.Role == UserRole.Seller)
                .Include(u => u.SellerProfile)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return View(sellers);
        }


        [HttpGet]
        public async Task<IActionResult> SellerDetails(int id)
        {
            var seller = await _context.Users
                .Include(u => u.SellerProfile)
                    .ThenInclude(s => s!.Products)
                .FirstOrDefaultAsync(u =>
                    u.UserId == id &&
                    u.Role == UserRole.Seller);

            if (seller == null)
                return NotFound();

            return View(seller);
        }


        // ============================================================
        // ACTIVATE / DEACTIVATE SELLER
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSellerStatus(int id)
        {
            var seller = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.UserId == id &&
                    u.Role == UserRole.Seller);

            if (seller == null)
                return NotFound();

            seller.IsActive = !seller.IsActive;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                seller.IsActive
                    ? "Seller account activated."
                    : "Seller account deactivated.";

            return RedirectToAction(nameof(Sellers));
        }


        // ============================================================
        // DELETE SELLER
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSeller(int id)
        {
            var seller = await _context.Users
                .Include(u => u.SellerProfile)
                .FirstOrDefaultAsync(u =>
                    u.UserId == id &&
                    u.Role == UserRole.Seller);

            if (seller == null)
                return NotFound();

            bool hasProducts = await _context.Products
                .AnyAsync(p =>
                    p.Seller.UserId == id);

            bool hasOrders = await _context.OrderItems
                .AnyAsync(oi =>
                    oi.Product.Seller.UserId == id);

            bool hasApplications = await _context.SellerApplications
                .AnyAsync(a =>
                    a.UserId == id);

            if (hasProducts ||
                hasOrders ||
                hasApplications)
            {
                TempData["ErrorMessage"] =
                    "This seller has existing business records and cannot be permanently deleted. Deactivate the seller instead.";

                return RedirectToAction(nameof(Sellers));
            }

            _context.Users.Remove(seller);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Seller deleted successfully.";

            return RedirectToAction(nameof(Sellers));
        }


        // ============================================================
        // ADMIN NOTIFICATIONS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Notifications()
        {
            int userId = GetCurrentUserId();

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(notifications);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
            int userId = GetCurrentUserId();

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n =>
                    n.NotificationId == id &&
                    n.UserId == userId);

            if (notification == null)
                return NotFound();

            notification.IsRead = true;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Notifications));
        }


        // ============================================================
        // CURRENT ADMIN USER ID
        // ============================================================

        private int GetCurrentUserId()
        {
            string? userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (!int.TryParse(userId, out int id))
                throw new UnauthorizedAccessException();

            return id;
        }
    }
}