using Ecom.Models;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Ecom.ViewModels
{
    public class SellerDashboardViewModel
    {
        public int TotalProducts { get; set; }
        public int ApprovedProducts { get; set; }
        public int PendingProducts { get; set; }
        public int RejectedProducts { get; set; }

        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int ProductsSold { get; set; }

        public decimal TotalRevenue { get; set; }

        public List<Product> RecentProducts { get; set; } = new();
        public List<Order> RecentOrders { get; set; } = new();
    }


    public class SellerAddProductViewModel
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int CategoryId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        public List<IFormFile> Images { get; set; } = new();

        public List<Category> Categories { get; set; } = new();
    }


    public class SellerEditProductViewModel
    {
        public int ProductId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int CategoryId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        public List<IFormFile> Images { get; set; } = new();

        public List<ProductImage> ExistingImages { get; set; } = new();

        public List<Category> Categories { get; set; } = new();
    }


    public class SellerProductListViewModel
    {
        public List<Product> Products { get; set; } = new();
    }
}