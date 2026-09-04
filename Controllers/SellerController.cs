using System.Security.Claims;
using Ecom.Models;
using Ecom.ViewModels;
using EcomDB.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecom.Controllers
{
    [Authorize(Roles = "Seller")]
    public class SellerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public SellerController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }


        // ============================================================
        // DASHBOARD
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int userId = GetCurrentUserId();

            SellerProfile? seller = await _context.SellerProfiles
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (seller == null)
                return RedirectToAction("CustomerLogin", "Account");

            List<Product> products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Where(p => p.SellerId == seller.SellerId)
                .ToListAsync();

            List<OrderItem> orderItems = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .Where(oi => oi.Product.SellerId == seller.SellerId)
                .ToListAsync();

            int totalOrders = orderItems
                .Select(oi => oi.OrderId)
                .Distinct()
                .Count();

            int pendingOrders = orderItems
                .Where(oi =>
                    oi.Order.OrderStatus == OrderStatus.Pending ||
                    oi.Order.OrderStatus == OrderStatus.Confirmed ||
                    oi.Order.OrderStatus == OrderStatus.Processing ||
                    oi.Order.OrderStatus == OrderStatus.Shipped)
                .Select(oi => oi.OrderId)
                .Distinct()
                .Count();

            int deliveredOrders = orderItems
                .Where(oi => oi.Order.OrderStatus == OrderStatus.Delivered)
                .Select(oi => oi.OrderId)
                .Distinct()
                .Count();

            int productsSold = orderItems
                .Where(oi => oi.Order.OrderStatus != OrderStatus.Cancelled)
                .Sum(oi => oi.Quantity);

            decimal revenue = orderItems
                .Where(oi => oi.Order.OrderStatus != OrderStatus.Cancelled)
                .Sum(oi => oi.UnitPrice * oi.Quantity);

            var model = new SellerDashboardViewModel
            {
                TotalProducts = products.Count,

                ApprovedProducts = products.Count(p =>
                    p.Status == ProductStatus.Approved),

                PendingProducts = products.Count(p =>
                    p.Status == ProductStatus.Pending),

                RejectedProducts = products.Count(p =>
                    p.Status == ProductStatus.Rejected),

                TotalOrders = totalOrders,
                PendingOrders = pendingOrders,
                DeliveredOrders = deliveredOrders,
                ProductsSold = productsSold,
                TotalRevenue = revenue,

                RecentProducts = products
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .ToList(),

                RecentOrders = orderItems
                    .Select(oi => oi.Order)
                    .DistinctBy(o => o.OrderId)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .ToList()
            };

            return View(model);
        }


        // ============================================================
        // ADD PRODUCT - GET
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> AddProduct()
        {
            var model = new SellerAddProductViewModel
            {
                Categories = await GetCategories()
            };

            return View(model);
        }


        // ============================================================
        // ADD PRODUCT - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(
            SellerAddProductViewModel model)
        {
            ValidateImages(model.Images);

            if (!ModelState.IsValid)
            {
                model.Categories = await GetCategories();
                return View(model);
            }

            int userId = GetCurrentUserId();

            SellerProfile? seller = await _context.SellerProfiles
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (seller == null)
                return RedirectToAction("CustomerLogin", "Account");

            bool categoryExists = await _context.Categories
                .AnyAsync(c => c.CategoryId == model.CategoryId);

            if (!categoryExists)
            {
                ModelState.AddModelError(
                    "CategoryId",
                    "Selected category does not exist.");

                model.Categories = await GetCategories();
                return View(model);
            }

            Product product = new Product
            {
                SellerId = seller.SellerId,
                CategoryId = model.CategoryId,
                Name = model.Name.Trim(),
                Description = model.Description.Trim(),
                Price = model.Price,
                StockQuantity = model.StockQuantity,
                Status = ProductStatus.Pending,
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);

            await _context.SaveChangesAsync();

            await SaveProductImages(product, model.Images);

            TempData["SuccessMessage"] =
                "Product submitted successfully. It is now waiting for admin approval.";

            return RedirectToAction(nameof(MyProducts));
        }


        // ============================================================
        // MY PRODUCTS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> MyProducts()
        {
            int userId = GetCurrentUserId();

            SellerProfile? seller = await _context.SellerProfiles
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (seller == null)
                return RedirectToAction("CustomerLogin", "Account");

            List<Product> products = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.OrderItems)
                .Where(p => p.SellerId == seller.SellerId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(new SellerProductListViewModel
            {
                Products = products
            });
        }


        // ============================================================
        // PRODUCT DETAILS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> ProductDetails(int id)
        {
            int userId = GetCurrentUserId();

            SellerProfile? seller = await _context.SellerProfiles
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (seller == null)
                return RedirectToAction("CustomerLogin", "Account");

            Product? product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.Reviews)
                .Include(p => p.OrderItems)
                    .ThenInclude(oi => oi.Order)
                .FirstOrDefaultAsync(p =>
                    p.ProductId == id &&
                    p.SellerId == seller.SellerId);

            if (product == null)
                return NotFound();

            return View(product);
        }


        // ============================================================
        // EDIT PRODUCT - GET
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            int userId = GetCurrentUserId();

            SellerProfile? seller = await _context.SellerProfiles
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (seller == null)
                return RedirectToAction("CustomerLogin", "Account");

            Product? product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p =>
                    p.ProductId == id &&
                    p.SellerId == seller.SellerId);

            if (product == null)
                return NotFound();

            var model = new SellerEditProductViewModel
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Description = product.Description,
                CategoryId = product.CategoryId,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                ExistingImages = product.ProductImages.ToList(),
                Categories = await GetCategories()
            };

            return View(model);
        }


        // ============================================================
        // EDIT PRODUCT - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(
            SellerEditProductViewModel model)
        {
            int userId = GetCurrentUserId();

            SellerProfile? seller = await _context.SellerProfiles
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (seller == null)
                return RedirectToAction("CustomerLogin", "Account");

            Product? product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p =>
                    p.ProductId == model.ProductId &&
                    p.SellerId == seller.SellerId);

            if (product == null)
                return NotFound();

            ValidateImages(model.Images, false);

            if (!ModelState.IsValid)
            {
                model.Categories = await GetCategories();
                model.ExistingImages = product.ProductImages.ToList();

                return View(model);
            }

            bool categoryExists = await _context.Categories
                .AnyAsync(c => c.CategoryId == model.CategoryId);

            if (!categoryExists)
            {
                ModelState.AddModelError(
                    "CategoryId",
                    "Selected category does not exist.");

                model.Categories = await GetCategories();
                model.ExistingImages = product.ProductImages.ToList();

                return View(model);
            }

            product.Name = model.Name.Trim();
            product.Description = model.Description.Trim();
            product.CategoryId = model.CategoryId;
            product.Price = model.Price;
            product.StockQuantity = model.StockQuantity;
            product.UpdatedAt = DateTime.UtcNow;

            // Any edited product must go through admin approval again.
            product.Status = ProductStatus.Pending;
            product.IsActive = false;
            product.RejectionReason = null;
            product.ReviewedAt = null;
            product.ReviewedByAdminId = null;

            // If new images were selected, replace the old images.
            if (model.Images != null && model.Images.Count > 0)
            {
                foreach (ProductImage oldImage in product.ProductImages.ToList())
                {
                    DeletePhysicalImage(oldImage.ImageUrl);

                    _context.ProductImages.Remove(oldImage);
                }

                await _context.SaveChangesAsync();

                await SaveProductImages(product, model.Images);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Product updated successfully and submitted for admin approval again.";

            return RedirectToAction(nameof(MyProducts));
        }


        // ============================================================
        // DELETE PRODUCT
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            int userId = GetCurrentUserId();

            SellerProfile? seller = await _context.SellerProfiles
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (seller == null)
                return RedirectToAction("CustomerLogin", "Account");

            Product? product = await _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.OrderItems)
                .FirstOrDefaultAsync(p =>
                    p.ProductId == id &&
                    p.SellerId == seller.SellerId);

            if (product == null)
                return NotFound();

            // Do not destroy customer order history.
            if (product.OrderItems.Any())
            {
                TempData["ErrorMessage"] =
                    "This product cannot be deleted because it is already associated with an order.";

                return RedirectToAction(nameof(MyProducts));
            }

            // Delete dependent records first because the database
            // relationships are configured with NoAction.
            var cartItems = await _context.CartItems
                .Where(x => x.ProductId == id)
                .ToListAsync();

            var wishlistItems = await _context.WishlistItems
                .Where(x => x.ProductId == id)
                .ToListAsync();

            var offerProducts = await _context.OfferProducts
                .Where(x => x.ProductId == id)
                .ToListAsync();

            var reviews = await _context.Reviews
                .Where(x => x.ProductId == id)
                .ToListAsync();

            _context.CartItems.RemoveRange(cartItems);
            _context.WishlistItems.RemoveRange(wishlistItems);
            _context.OfferProducts.RemoveRange(offerProducts);
            _context.Reviews.RemoveRange(reviews);

            foreach (ProductImage image in product.ProductImages)
            {
                DeletePhysicalImage(image.ImageUrl);
            }

            _context.ProductImages.RemoveRange(product.ProductImages);
            _context.Products.Remove(product);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Product deleted successfully.";

            return RedirectToAction(nameof(MyProducts));
        }


        // ============================================================
        // LOGOUT
        // ============================================================

        [HttpGet]
        public IActionResult Logout()
        {
            return RedirectToAction("Logout", "Account");
        }


        // ============================================================
        // HELPERS
        // ============================================================

        private async Task<List<Category>> GetCategories()
        {
            return await _context.Categories
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
        }


        private void ValidateImages(
            List<IFormFile>? images,
            bool required = true)
        {
            if (images == null || images.Count == 0)
            {
                if (required)
                {
                    ModelState.AddModelError(
                        "Images",
                        "Please upload at least one product image.");
                }

                return;
            }

            string[] allowedExtensions =
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

            foreach (IFormFile image in images)
            {
                if (image == null || image.Length == 0)
                {
                    ModelState.AddModelError(
                        "Images",
                        "One of the selected images is empty.");

                    continue;
                }

                if (image.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        "Images",
                        $"Image '{image.FileName}' is larger than 5 MB.");

                    continue;
                }

                string extension =
                    Path.GetExtension(image.FileName)
                        .ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        "Images",
                        $"Image '{image.FileName}' has an unsupported format.");
                }
            }
        }


        private async Task SaveProductImages(
            Product product,
            List<IFormFile> images)
        {
            string uploadFolder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "products");

            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            foreach (IFormFile image in images)
            {
                string extension =
                    Path.GetExtension(image.FileName)
                        .ToLowerInvariant();

                string uniqueFileName =
                    $"{Guid.NewGuid():N}{extension}";

                string filePath =
                    Path.Combine(
                        uploadFolder,
                        uniqueFileName);

                await using FileStream stream =
                    new FileStream(
                        filePath,
                        FileMode.Create);

                await image.CopyToAsync(stream);

                _context.ProductImages.Add(
                    new ProductImage
                    {
                        ProductId = product.ProductId,
                        ImageUrl =
                            $"/uploads/products/{uniqueFileName}"
                    });
            }

            await _context.SaveChangesAsync();
        }


        private void DeletePhysicalImage(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return;

            string relativePath = imageUrl.TrimStart('/')
                .Replace('/', Path.DirectorySeparatorChar);

            string fullPath = Path.Combine(
                _environment.WebRootPath,
                relativePath.Replace(
                    "uploads" + Path.DirectorySeparatorChar,
                    "uploads" + Path.DirectorySeparatorChar));

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }


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