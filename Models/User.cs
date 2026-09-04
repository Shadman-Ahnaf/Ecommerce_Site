using System.ComponentModel.DataAnnotations;

namespace Ecom.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        public string? Phone { get; set; }

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public UserRole Role { get; set; } = UserRole.Buyer;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public Admin? Admin { get; set; }

        public ICollection<SellerApplication> SellerApplications { get; set; }
            = new List<SellerApplication>();

        public SellerProfile? SellerProfile { get; set; }

        public Cart? Cart { get; set; }

        public Wishlist? Wishlist { get; set; }

        public ICollection<Order> Orders { get; set; }
            = new List<Order>();

        public ICollection<Review> Reviews { get; set; }
            = new List<Review>();

        public ICollection<Notification> Notifications { get; set; }
            = new List<Notification>();

        public ICollection<RefreshToken> RefreshTokens { get; set; }
            = new List<RefreshToken>();
    }

    public enum UserRole
    {
        Buyer,
        Seller,
        Admin
    }
}