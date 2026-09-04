using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ecom.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required]
        public int SellerId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [ForeignKey(nameof(SellerId))]
        public SellerProfile Seller { get; set; } = null!;

        [ForeignKey(nameof(CategoryId))]
        public Category Category { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        public ProductStatus Status { get; set; } = ProductStatus.Pending;

        public bool IsActive { get; set; } = false;

        public string? RejectionReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? ReviewedAt { get; set; }

        public int? ReviewedByAdminId { get; set; }

        // Navigation Properties

        public ICollection<ProductImage> ProductImages { get; set; }
            = new List<ProductImage>();

        public ICollection<CartItem> CartItems { get; set; }
            = new List<CartItem>();

        public ICollection<OrderItem> OrderItems { get; set; }
            = new List<OrderItem>();

        public ICollection<Review> Reviews { get; set; }
            = new List<Review>();

        public ICollection<WishlistItem> WishlistItems { get; set; }
            = new List<WishlistItem>();

        public ICollection<OfferProduct> OfferProducts { get; set; }
            = new List<OfferProduct>();
    }

    public enum ProductStatus
    {
        Pending,
        Approved,
        Rejected
    }
}