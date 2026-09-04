
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ecom.Models
{
    public class SellerProfile
    {
        [Key]
        public int SellerId { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string BusinessName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string BusinessDescription { get; set; } = string.Empty;

        public DateTime ApprovedDate { get; set; }

        // Navigation Properties
        public ICollection<Product> Products { get; set; }
            = new List<Product>();
    }
}
