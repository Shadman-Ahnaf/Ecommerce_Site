using Ecom.ViewModels;
using EcomDB.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecom.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // PUBLIC LANDING PAGE
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var newProducts = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Where(p =>
                    p.IsActive &&
                    p.StockQuantity > 0)
                .OrderByDescending(p => p.CreatedAt)
                .Take(4)
                .ToListAsync();

            var model = new LandingPageViewModel
            {
                NewProducts = newProducts
            };

            return View(model);
        }


        // ============================================================
        // CUSTOMER SHOP
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Shop(
            string? search,
            int? categoryId,
            decimal? minPrice,
            decimal? maxPrice,
            int? dateFilter)
        {
            var query = _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.Reviews)
                .Where(p =>
                    p.IsActive &&
                    p.StockQuantity > 0);

            // --------------------------------------------------------
            // SEARCH
            // --------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    p.Description.Contains(search));
            }


            // --------------------------------------------------------
            // CATEGORY
            // --------------------------------------------------------

            if (categoryId.HasValue)
            {
                query = query.Where(p =>
                    p.CategoryId == categoryId.Value);
            }


            // --------------------------------------------------------
            // MINIMUM PRICE
            // --------------------------------------------------------

            if (minPrice.HasValue)
            {
                query = query.Where(p =>
                    p.Price >= minPrice.Value);
            }


            // --------------------------------------------------------
            // MAXIMUM PRICE
            // --------------------------------------------------------

            if (maxPrice.HasValue)
            {
                query = query.Where(p =>
                    p.Price <= maxPrice.Value);
            }


            // --------------------------------------------------------
            // DATE FILTER
            //
            // 7  = Last 1 Week
            // 30 = Last 1 Month
            // --------------------------------------------------------

            DateTime now = DateTime.UtcNow;

            if (dateFilter == 7)
            {
                DateTime oneWeekAgo =
                    now.AddDays(-7);

                query = query.Where(p =>
                    p.CreatedAt >= oneWeekAgo);
            }
            else if (dateFilter == 30)
            {
                DateTime oneMonthAgo =
                    now.AddDays(-30);

                query = query.Where(p =>
                    p.CreatedAt >= oneMonthAgo);
            }


            // --------------------------------------------------------
            // GET FILTERED PRODUCTS
            // --------------------------------------------------------

            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();


            // --------------------------------------------------------
            // CATEGORIES
            // --------------------------------------------------------

            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.CategoryName)
                .ToListAsync();


            // --------------------------------------------------------
            // ACTIVE OFFERS
            // --------------------------------------------------------

            var offers = await _context.Offers
                .AsNoTracking()
                .Where(o =>
                    o.IsActive &&
                    o.StartDate <= now &&
                    o.EndDate >= now)
                .OrderByDescending(o => o.DiscountPercent)
                .ToListAsync();


            // --------------------------------------------------------
            // HOT PRODUCTS
            // Based primarily on review count
            // --------------------------------------------------------

            var hotProducts = products
                .OrderByDescending(p => p.Reviews.Count)
                .ThenByDescending(p => p.CreatedAt)
                .Take(8)
                .ToList();


            // --------------------------------------------------------
            // RECENT PRODUCTS
            // --------------------------------------------------------

            var newProducts = products
                .OrderByDescending(p => p.CreatedAt)
                .Take(8)
                .ToList();


            // --------------------------------------------------------
            // VIEW MODEL
            // --------------------------------------------------------

            var model = new HomeViewModel
            {
                SearchResults = products,

                NewProducts = newProducts,

                HotProducts = hotProducts,

                Categories = categories,

                ActiveOffers = offers,

                Search = search,

                CategoryId = categoryId,

                MinPrice = minPrice,

                MaxPrice = maxPrice,

                DateFilter = dateFilter
            };

            return View(model);
        }
    }
}