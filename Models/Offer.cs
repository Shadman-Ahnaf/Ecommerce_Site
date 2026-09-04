using System.ComponentModel.DataAnnotations;

namespace Ecom.Models
{
    public class Offer
    {
        [Key]
        public int OfferId { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0, 100)]
        public decimal DiscountPercent { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public ICollection<OfferProduct> OfferProducts { get; set; }
            = new List<OfferProduct>();
    }
}