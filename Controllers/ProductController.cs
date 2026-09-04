using System.Security.Claims;
using Ecom.ViewModels;
using EcomDB.Data;
using Ecom.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecom.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        // PRODUCT DETAILS
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p =>
                    p.ProductId == id &&
                    p.IsActive);

            if (product == null)
                return NotFound();

            var seller = await _context.SellerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SellerId == product.SellerId);

            string sellerName = "Unknown Seller";

            if (seller != null)
            {
                var sellerUser = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == seller.UserId);

                if (sellerUser != null)
                    sellerName = sellerUser.Name;
            }

            var reviews = await _context.Reviews
                .AsNoTracking()
                .Include(r => r.User)
                .Where(r => r.ProductId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var relatedProducts = await _context.Products
                .AsNoTracking()
                .Include(p => p.ProductImages)
                .Include(p => p.Category)
                .Where(p =>
                    p.IsActive &&
                    p.StockQuantity > 0 &&
                    p.CategoryId == product.CategoryId &&
                    p.ProductId != product.ProductId)
                .OrderByDescending(p => p.CreatedAt)
                .Take(4)
                .ToListAsync();

            bool hasReviewed = false;

            if (User.Identity?.IsAuthenticated == true)
            {
                string? claim = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (int.TryParse(claim, out int userId))
                {
                    hasReviewed = await _context.Reviews
                        .AnyAsync(r =>
                            r.ProductId == id &&
                            r.UserId == userId);
                }
            }

            var model = new ProductDetailsViewModel
            {
                Product = product,
                SellerName = sellerName,
                Reviews = reviews,
                RelatedProducts = relatedProducts,
                HasReviewed = hasReviewed,
                NewReview = new Review
                {
                    ProductId = id
                }
            };

            return View(model);
        }

        // ADD REVIEW
        [Authorize(Roles = "Buyer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(
            int productId,
            int rating,
            string comment)
        {
            string? claim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(claim, out int userId))
                return RedirectToAction("CustomerLogin", "Account");

            var product = await _context.Products
                .FirstOrDefaultAsync(p =>
                    p.ProductId == productId &&
                    p.IsActive);

            if (product == null)
                return NotFound();

            if (rating < 1 || rating > 5)
            {
                TempData["ReviewError"] = "Please select a rating between 1 and 5.";
                return RedirectToAction(nameof(Details), new { id = productId });
            }

            if (string.IsNullOrWhiteSpace(comment))
            {
                TempData["ReviewError"] = "Please write a review.";
                return RedirectToAction(nameof(Details), new { id = productId });
            }

            comment = comment.Trim();

            if (comment.Length > 500)
            {
                TempData["ReviewError"] = "Your review cannot exceed 500 characters.";
                return RedirectToAction(nameof(Details), new { id = productId });
            }

            bool alreadyReviewed = await _context.Reviews
                .AnyAsync(r =>
                    r.ProductId == productId &&
                    r.UserId == userId);

            if (alreadyReviewed)
            {
                TempData["ReviewError"] = "You have already reviewed this product.";
                return RedirectToAction(nameof(Details), new { id = productId });
            }

            var review = new Review
            {
                ProductId = productId,
                UserId = userId,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["ReviewMessage"] = "Thank you! Your review has been added.";

            return RedirectToAction(nameof(Details), new { id = productId });
        }
    }
}